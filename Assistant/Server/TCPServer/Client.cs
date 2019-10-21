using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Server.TCPServer.Commands;
using Assistant.Server.TCPServer.Events;
using Assistant.Server.TCPServer.Responses;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;
using static Assistant.Server.TCPServer.CommandEnums;

namespace Assistant.Server.TCPServer {
	public class Client {
		private readonly Logger Logger = new Logger("CLIENT");
		private CommandObject? PreviousCommand;
		internal (int?, Thread?) ThreadInfo;
		public string? UniqueId { get; private set; }
		public string? IpAddress { get; set; }
		public Socket ClientSocket { get; set; }
		public bool DisconnectConnection { get; set; }
		public EndPoint? ClientEndPoint { get; set; }

		public delegate void OnClientMessageRecevied(object sender, ClientMessageEventArgs e);
		public delegate void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e);
		public delegate void OnClientCommandRecevied(object sender, ClientCommandReceviedEventArgs e);
		public event OnClientMessageRecevied? OnMessageRecevied;
		public event OnClientDisconnected? OnDisconnected;
		public event OnClientCommandRecevied? OnCommandRecevied;

		public Client(Socket sock) {
			ClientSocket = sock ?? throw new ArgumentNullException(nameof(sock), "Socket cannot be null!");
			ClientEndPoint = ClientSocket.RemoteEndPoint;
			IpAddress = ClientEndPoint?.ToString()?.Split(':')[0].Trim();

			UniqueId = IpAddress != null && !IpAddress.IsNull()
				? GenerateUniqueId(IpAddress)
				: GenerateUniqueId(ClientSocket.GetHashCode().ToString());
		}

		public async Task Init() {
			Logger.Log($"Connected client IP => {IpAddress} / {UniqueId}", LogLevels.Info);
			TCPServerCore.AddClient(this);
			await RecevieAsync().ConfigureAwait(false);
		}

		public async Task DisconnectClientAsync(bool dispose = false) {
			DisconnectConnection = true;

			if (ClientSocket == null) {
				return;
			}

			if (ClientSocket.Connected) {
				ClientSocket.Disconnect(true);
			}

			while (ClientSocket.Connected) {
				Logger.Log("Waiting for client to disconnect...");
				await Task.Delay(5).ConfigureAwait(false);
			}

			Logger.Log($"Disconnected client -> {UniqueId} / {IpAddress}");

			if (IpAddress != null && UniqueId != null) {
				OnDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(IpAddress, UniqueId, 5000));
			}

			if (dispose) {
				ClientSocket?.Close();
				ClientSocket?.Dispose();

				Helpers.ScheduleTask(() => {
					TCPServerCore.RemoveClient(this);
				}, TimeSpan.FromSeconds(5));
			}
		}

		private async Task RecevieAsync() {
			while (!DisconnectConnection) {
				try {
					if (ClientSocket.Available <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					if (!ClientSocket.Connected) {
						await DisconnectClientAsync(true).ConfigureAwait(false);
					}

					byte[] buffer = new byte[5024];
					int i = ClientSocket.Receive(buffer);

					if (i <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					string receviedMessage = Encoding.ASCII.GetString(buffer);
					receviedMessage = Regex.Replace(receviedMessage, "\\0", string.Empty);

					if (string.IsNullOrEmpty(receviedMessage)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					CommandObject cmdObject = new CommandObject(receviedMessage, DateTime.Now);

					if (PreviousCommand != null && cmdObject.Equals(PreviousCommand)) {
						continue;
					}

					OnMessageRecevied?.Invoke(this, new ClientMessageEventArgs(receviedMessage));

					await OnRecevied(cmdObject).ConfigureAwait(false);
					PreviousCommand = cmdObject;
					await Task.Delay(1).ConfigureAwait(false);
				}
				catch (SocketException) {
					//this means client was forcefully disconnected from the remote endpoint. so process it here and disconnect the client and dispose
					await DisconnectClientAsync().ConfigureAwait(false);
					break;
				}
				catch (ThreadAbortException) {
					//This means the client is disconnected and the threat is aborted.
					break;
				}
				catch (Exception e) {
					Logger.Log(e);
					continue;
				}
			}
		}

		private async Task OnRecevied(CommandObject cmdObject) {
			if (cmdObject == null || string.IsNullOrEmpty(cmdObject.ReceviedMessageObject)) {
				return;
			}

			await ProcessMessage(cmdObject).ConfigureAwait(false);
		}

		private async Task ProcessMessage(CommandObject cmdObject) {
			if (cmdObject == null) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			CommandBase cmdBase = JsonConvert.DeserializeObject<CommandBase>(cmdObject.ReceviedMessageObject);

			if (cmdBase == null) {
				return;
			}

			OnCommandRecevied?.Invoke(this, new ClientCommandReceviedEventArgs(cmdBase.CommandTime, cmdBase));

			string? response = cmdBase.CommandType switch
			{
				CommandType.Gpio => OnGpioCommandType(cmdBase),
				CommandType.Remainder => OnRemainderCommandType(cmdBase),
				CommandType.Alarm => OnAlarmCommandType(cmdBase),
				CommandType.Weather => OnWeatherCommandType(cmdBase),
				CommandType.Client => OnClientCommandType(cmdBase),
				_ => FormatResponse(CommandResponseCode.INVALID, ResponseObjectType.NoResponse, "Invalid Command!"),
			};

			if (string.IsNullOrEmpty(response)) {
				await SendResponseAsync(FormatResponse(CommandResponseCode.INVALID, ResponseObjectType.NoResponse, "Invalid Command!")).ConfigureAwait(false);
				return;
			}

			await SendResponseAsync(response).ConfigureAwait(false);
		}

		private string? OnClientCommandType(CommandBase command) {
			if (command == null) {
				return null;
			}

			if (command.Command == Command.InvalidCommand) {
				return null;
			}

			Logger.Log("Client command recevied!");
			switch (command.Command) {
				case Command.Disconnect:
					Helpers.ScheduleTask(async () => await DisconnectClientAsync(true).ConfigureAwait(false), TimeSpan.FromSeconds(2));
					return FormatResponse(CommandResponseCode.OK, ResponseObjectType.NoResponse, "Disconnecting in 2 seconds...");
				case Command.Initiate:
					return FormatResponse(CommandResponseCode.OK, ResponseObjectType.NoResponse, "Successfully connected!");
				default:
					return null;
			}
		}

		private string? OnGpioCommandType(CommandBase command) {
			if (command == null) {
				return null;
			}

			if (command.Command == Command.InvalidCommand) {
				return null;
			}

			Logger.Log("Gpio command recevied!");
			switch (command.Command) {
				case Command.GetGpioAll:
					GetPins getPins = new GetPins();
					string? pinsConfig = getPins.GetJson();

					if (string.IsNullOrEmpty(pinsConfig)) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to fetch pin configuration");
					}

					return FormatResponse(CommandResponseCode.OK, ResponseObjectType.GetGpioAll, "Success!", pinsConfig);

				case Command.GetGpio when !string.IsNullOrEmpty(command.CommandParametersJson) && command.CommandParametersObject != null:
					GetGpio? getRequest = command.CommandParametersObject as GetGpio;

					if (getRequest == null) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse);
					}

					GpioPinConfig? pinConfig = Core.PiController?.GetPinController().GetGpioConfig(getRequest.PinNumber);

					if (pinConfig == null) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to fetch pin configuration");
					}

					string result = GpioPinConfig.AsJson(pinConfig);

					if (string.IsNullOrEmpty(result)) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to produce output result.");
					}

					return FormatResponse(CommandResponseCode.OK, ResponseObjectType.GetGpio, "Success!", result);

				case Command.SetGpioGeneral when !string.IsNullOrEmpty(command.CommandParametersJson) && command.CommandParametersObject != null:
					SetGpio? setRequest = command.CommandParametersObject as SetGpio;

					if (setRequest == null) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse);
					}

					bool? success = Core.PiController?.GetPinController().SetGpioValue(setRequest.PinNumber, setRequest.PinMode, setRequest.PinState);

					if (success.HasValue && success.Value) {
						return FormatResponse(CommandResponseCode.OK, ResponseObjectType.NoResponse, "Successfully set the pin configuration.");
					}

					return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to set pin configuration.");

				case Command.SetGpioDelayed when !string.IsNullOrEmpty(command.CommandParametersJson) && command.CommandParametersObject != null:

					SetGpioDelayed? setDelayedRequest = command.CommandParametersObject as SetGpioDelayed;

					if (setDelayedRequest == null) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse);
					}

					bool? isSuccess = Core.PiController?.GetPinController().SetGpioWithTimeout(setDelayedRequest.PinNumber, setDelayedRequest.PinMode, setDelayedRequest.PinState, TimeSpan.FromMinutes(setDelayedRequest.Delay));

					if (isSuccess.HasValue && isSuccess.Value) {
						return FormatResponse(CommandResponseCode.OK, ResponseObjectType.NoResponse, $"Successfully configured the pin with delay of {setDelayedRequest.Delay} minutes.");
					}

					return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to set pin configuration with delay.");

				default:
					return null;
			}
		}

		private string? OnRemainderCommandType(CommandBase command) {
			if (command == null) {
				return null;
			}

			if (command.Command == Command.InvalidCommand) {
				return null;
			}

			Logger.Log("Remainder command recevied!");
			switch (command.Command) {
				case Command.SetRemainder when !string.IsNullOrEmpty(command.CommandParametersJson) && command.CommandParametersObject != null:
					SetRemainder? remainderRequest = command.CommandParametersObject as SetRemainder;

					if (remainderRequest == null) {
						return FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to set remainder.");
					}

					return Core.RemainderManager.Remind(remainderRequest.RemainderMessage, remainderRequest.RemainderDelay) ?
						FormatResponse(CommandResponseCode.OK, ResponseObjectType.NoResponse, "Successfully set remainder.") :
						FormatResponse(CommandResponseCode.FAIL, ResponseObjectType.NoResponse, "Failed to set remainder.");
				default:
					return null;
			}
		}

		//TODO: Alarm command
		private string? OnAlarmCommandType(CommandBase command) {
			if (command == null) {
				return null;
			}

			return null;
		}

		//TODO: Weather command
		private string? OnWeatherCommandType(CommandBase command) {
			if (command == null) {
				return null;
			}

			return null;
		}

		public async Task SendResponseAsync(string? response) {
			if (string.IsNullOrEmpty(response)) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			ClientSocket.Send(Encoding.ASCII.GetBytes(response));
		}

		private static string GenerateUniqueId(string ipAddress) {
			if (Helpers.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}

		private static string FormatResponse(CommandResponseCode responseCode, ResponseObjectType respType, string? msg = null, string? json = null) {
			ResponseBase response = new ResponseBase(responseCode, respType, msg, json);
			return response.AsJson();
		}
	}
}
