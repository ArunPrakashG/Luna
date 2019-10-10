using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Log;
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
		public string? UniqueId { get; private set; }
		public string? IpAddress { get; set; }
		public Socket ClientSocket { get; set; }
		public bool DisconnectConnection { get; set; }
		public EndPoint? ClientEndPoint { get; set; }
		private readonly Logger Logger = new Logger("CLIENT");
		internal (int?, Thread?) ThreadInfo;

		public delegate void OnClientMessageRecevied(object sender, ClientMessageEventArgs e);
		public delegate void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e);
		public delegate void OnClientCommandRecevied(object sender, ClientCommandReceviedEventArgs e);
		public event OnClientMessageRecevied? OnMessageRecevied;
		public event OnClientDisconnected? OnDisconnected;
		public event OnClientCommandRecevied? OnCommandRecevied;
		private ClientPayload CachedPayload { get; set; } = new ClientPayload() {
			RawMessage = string.Empty
		};

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

			if (ClientSocket != null) {
				ClientSocket.Disconnect(true);
			}

			if (ClientSocket != null) {
				while (ClientSocket.Connected) {
					Logger.Log("Waiting for client to disconnect...");
					await Task.Delay(5).ConfigureAwait(false);
				}
			}

			Logger.Log($"Disconnected client -> {UniqueId} / {IpAddress}");
			if (IpAddress != null && UniqueId != null) {
				OnDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(IpAddress, UniqueId, 5000));
			}

			if (dispose) {
				if (ClientSocket != null) {
					ClientSocket.Close();
					ClientSocket.Dispose();
				}

				Helpers.ScheduleTask(() => {
					TCPServerCore.RemoveClient(this);
				}, TimeSpan.FromSeconds(5));
			}
		}

		private async Task RecevieAsync() {
			while (!DisconnectConnection) {
				try {
					if (ClientSocket.Available <= 0) {
						await Task.Delay(2).ConfigureAwait(false);
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

					if (string.IsNullOrEmpty(receviedMessage)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					receviedMessage = Regex.Replace(receviedMessage, "\\0", string.Empty);

					ClientPayload payload = new ClientPayload() {
						RawMessage = receviedMessage,
						ClientId = UniqueId,
						ClientIp = IpAddress,
						ReceviedTime = DateTime.Now
					};

					if (payload.RawMessage.Equals(CachedPayload.RawMessage, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					await OnRecevied(payload).ConfigureAwait(false);
					CachedPayload = payload;
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

		private async Task OnRecevied(ClientPayload payload) {
			if (payload == null || payload.RawMessage == null || Helpers.IsNullOrEmpty(payload.RawMessage)) {
				return;
			}

			OnMessageRecevied?.Invoke(this, new ClientMessageEventArgs(payload));
			await ProcessMessage(payload).ConfigureAwait(false);
		}

		private async Task ProcessMessage(ClientPayload payload) {
			if (payload == null) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			if (string.IsNullOrEmpty(payload.RawMessage) || string.IsNullOrEmpty(payload.ClientIp) || string.IsNullOrEmpty(payload.ClientId)) {
				return;
			}

			CommandStructure? command = ParseCommand(payload.RawMessage);

			if (command == null) {
				Logger.Log("Command parsing failed.", LogLevels.Warn);
				return;
			}

			OnCommandRecevied?.Invoke(this, new ClientCommandReceviedEventArgs(command.CommandType, command.Command, command.CommandArguments));
			string? response = command.CommandType switch
			{
				CommandType.Gpio => OnGpioCommandType(command),
				CommandType.Remainder => OnRemainderCommandType(command),
				CommandType.Alarm => OnAlarmCommandType(command),
				CommandType.Weather => OnWeatherCommandType(command),
				CommandType.Client => OnClientCommandType(command),
				_ => "Invalid Command!",
			};

			if (string.IsNullOrEmpty(response)) {
				await SendResponseAsync("The response is null!").ConfigureAwait(false);
				return;
			}

			await SendResponseAsync(response).ConfigureAwait(false);
		}

		private string? OnClientCommandType(CommandStructure? command) {
			if (command == null) {
				return null;
			}

			if (command.Command == Command.InvalidCommand) {
				return FormatResponse(CommandResponseCode.INVALID);
			}

			switch (command.Command) {
				case Command.Disconnect:
					Helpers.ScheduleTask(async () => await DisconnectClientAsync(true).ConfigureAwait(false), TimeSpan.FromSeconds(2));
					return FormatResponse(CommandResponseCode.OK, "Disconnecting in 2 seconds...");
			}

			return FormatResponse(CommandResponseCode.INVALID);
		}

		private string? OnGpioCommandType(CommandStructure? command) {
			if (command == null) {
				return null;
			}

			if (command.CommandArguments == null || command.CommandArguments.Length <= 0) {
				return FormatResponse(CommandResponseCode.INVALID, "The arguments are invalid");
			}

			if (command.Command == Command.InvalidCommand) {
				return FormatResponse(CommandResponseCode.INVALID);
			}

			int pin = 0;
			GpioPinMode pinMode;
			GpioPinState pinState;

			switch (command.Command) {
				case Command.GetGpio when command.CommandArguments.Length <= 0:
					return FormatResponse(CommandResponseCode.INVALID);

				case Command.GetGpio when command.CommandArguments.Length == 1:
					int pinNumber = Convert.ToInt32(command.CommandArguments[0].Trim());
					GpioPinConfig? pinConfig = Core.PiController?.GetPinController().GetGpioConfig(pinNumber);

					if (pinConfig == null) {
						return FormatResponse(CommandResponseCode.FAIL, "Failed to fetch pin configuration");
					}

					string result = GpioPinConfig.AsJson(pinConfig);

					if (string.IsNullOrEmpty(result)) {
						return FormatResponse(CommandResponseCode.FAIL, "Failed to produce output result.");
					}

					return result;

				case Command.SetGpioGeneral when command.CommandArguments.Length < 3:
					return FormatResponse(CommandResponseCode.INVALID);

				case Command.SetGpioGeneral when command.CommandArguments.Length == 3:
					pinMode = (GpioPinMode) Convert.ToInt32(command.CommandArguments[1].Trim());
					pinState = (GpioPinState) Convert.ToInt32(command.CommandArguments[2].Trim());
					bool? success = Core.PiController?.GetPinController().SetGpioValue(pin, pinMode, pinState);
					if (success.HasValue && success.Value) {
						return FormatResponse(CommandResponseCode.OK);
					}
					else {
						return FormatResponse(CommandResponseCode.FAIL);
					}

				case Command.SetGpioDelayed when command.CommandArguments.Length < 4:
					return FormatResponse(CommandResponseCode.INVALID);

				case Command.SetGpioDelayed when command.CommandArguments.Length == 4:
					pinMode = (GpioPinMode) Convert.ToInt32(command.CommandArguments[1].Trim());
					pinState = (GpioPinState) Convert.ToInt32(command.CommandArguments[2].Trim());
					int timeDelay = Convert.ToInt32(command.CommandArguments[3].Trim());
					bool? isSuccess = Core.PiController?.GetPinController().SetGpioWithTimeout(pin, pinMode, pinState, TimeSpan.FromMinutes(timeDelay));
					if (isSuccess.HasValue && isSuccess.Value) {
						return FormatResponse(CommandResponseCode.OK, $"Successfully configured the pin with delay of {timeDelay} minutes.");
					}
					else {
						return FormatResponse(CommandResponseCode.FAIL);
					}
			}

			return FormatResponse(CommandResponseCode.FATAL);
		}

		private string? OnRemainderCommandType(CommandStructure? command) {
			if (command == null) {
				return null;
			}

			if (command.CommandArguments == null || command.CommandArguments.Length <= 0) {
				return FormatResponse(CommandResponseCode.INVALID, "The arguments are invalid");
			}

			if (command.Command == Command.InvalidCommand) {
				return FormatResponse(CommandResponseCode.INVALID);
			}

			string? remainderMessage = null;
			int remainderDelay;

			switch (command.Command) {
				case Command.SetRemainder when command.CommandArguments.Length < 2:
					return FormatResponse(CommandResponseCode.INVALID);

				case Command.SetRemainder when command.CommandArguments.Length == 2:
					remainderMessage = command.CommandArguments[0].Trim();
					remainderDelay = Convert.ToInt32(command.CommandArguments[1]);

					return Core.RemainderManager.Remind(remainderMessage, remainderDelay) ?
						FormatResponse(CommandResponseCode.OK) :
						FormatResponse(CommandResponseCode.FAIL);
			}

			return FormatResponse(CommandResponseCode.INVALID);
		}

		private string? OnAlarmCommandType(CommandStructure? command) {
			if (command == null) {
				return null;
			}

			return null;
		}

		private string? OnWeatherCommandType(CommandStructure? command) {
			if (command == null) {
				return null;
			}

			return null;
		}

		private CommandStructure? ParseCommand(string? rawMessage) {
			if (string.IsNullOrEmpty(rawMessage)) {
				return null;
			}

			CommandStructure structure = new CommandStructure();

			string[]? splitted = rawMessage.Split('|');

			if (splitted == null || splitted.Length <= 0) {
				return null;
			}

			if (string.IsNullOrEmpty(splitted[0])) {
				Logger.Log("Command type isn't specified.", LogLevels.Warn);
				return null;
			}

			if (string.IsNullOrEmpty(splitted[1])) {
				Logger.Log("Command isn't specified.", LogLevels.Warn);
				return null;
			}

			//COMMAND_TYPE|COMMAND|ARGS1~ARGS2~ARGS3~ARGS4 ....

			int? commandType = Convert.ToInt32(splitted[0].Trim());
			int? command = Convert.ToInt32(splitted[1].Trim());

			if (splitted[2] != null && !string.IsNullOrEmpty(splitted[2])) {
				structure.CommandArguments = splitted[2].Contains('~') ? splitted[2].Split('~') : (new string[] { splitted[2] });
			}

			structure.CommandType = (CommandType) commandType;
			structure.Command = (Command) command;

			if (structure.CommandType == CommandType.InvalidType || structure.Command == Command.InvalidCommand) {
				Logger.Log("Invalid command recevied.", LogLevels.Warn);
				return null;
			}

			return structure;
		}

		public async Task SendResponseAsync(string response) {
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

		private string FormatResponse(CommandResponseCode responseCode, string? msg = null) {
			if (string.IsNullOrEmpty(msg)) {
				return $"{(int) responseCode}|{responseCode.ToString()}";
			}

			return $"{(int) responseCode}|{msg}";
		}
	}
}
