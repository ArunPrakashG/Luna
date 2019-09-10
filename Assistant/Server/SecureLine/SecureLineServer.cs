using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Server.SecureLine.Requests;
using Assistant.Server.SecureLine.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.Server.SecureLine {
	public class ConnectedClient {
		public string IPAddress { get; set; }
	}

	public class SecureLineServer : IDisposable {
		private TcpListener Listener { get; set; }
		private static int ServerPort { get; set; }
		private static IPAddress ListerningAddress { get; set; } = IPAddress.Any;
		private static IPEndPoint EndPoint { get; set; }
		public static bool IsOnline { get; private set; }
		public static bool StopServer { get; set; }
		private readonly Logger Logger = new Logger("SECURE-LINE");

		public static List<ConnectedClient> ConnectedClients = new List<ConnectedClient>();
		private static readonly SemaphoreSlim ProcessingSemaphore = new SemaphoreSlim(1, 1);

		public SecureLineServer(IPAddress address, int port) {
			ListerningAddress = address ?? throw new ArgumentNullException(nameof(address));
			ServerPort = port <= 0 ? throw new ArgumentNullException(nameof(port)) : port;
			EndPoint = new IPEndPoint(ListerningAddress, ServerPort);
		}

		public SecureLineServer() {
			throw new NotSupportedException("Initilize server with parameters!");
		}

		public async Task<bool> StartSecureLine(TimeSpan disposingTime) {
			Logger.Log("Starting secure line server...", Enums.LogLevels.Info);
			Listener = new TcpListener(EndPoint);
			Listener.Start(10);
			IsOnline = true;

			try {
				while (!StopServer && Listener != null) {
					Socket socket = await Listener.AcceptSocketAsync().ConfigureAwait(false);
					byte[] receiveBuffer = new byte[5024];
					int b = socket.Receive(receiveBuffer);
					EndPoint remoteEndpoint = socket.RemoteEndPoint;
					ConnectedClient client = new ConnectedClient() {
						IPAddress = remoteEndpoint.ToString().Split(':')[0].Trim()
					};
					Logger.Log($"Connected client IP => {client.IPAddress}", Enums.LogLevels.Trace);

					if (!ConnectedClients.Contains(client)) {
						ConnectedClients.Add(client);
					}

					string recevied = Encoding.ASCII.GetString(receiveBuffer, 0, b);
					string resultObject = await OnReceviedAsync(recevied).ConfigureAwait(false);

					if (!Helpers.IsNullOrEmpty(resultObject)) {
						socket.Send(Encoding.ASCII.GetBytes(ObjectToString<SuccessCommand>(new SuccessCommand() {
							ResponseCode = 0x00,
							ResponseMessage = "Success",
							JsonResponseObject = resultObject
						})));
					}
					else {
						socket.Send(Encoding.ASCII.GetBytes(ObjectToString<FailedCommand>(new FailedCommand() {
							ResponseCode = 0x01,
							FailReason = "The processed result is null!"
						})));
					}

					socket.Close();
				}
			}
			catch (Exception e) {
				Logger.Log(e);
			}
			finally {
				Listener.Stop();
			}

			return true;
		}

		private async Task<string> OnReceviedAsync(string recevied) {
			if (Helpers.IsNullOrEmpty(recevied)) {
				return null;
			}

			BaseRequest baseRequest;
			try {
				baseRequest = ObjectFromString<BaseRequest>(recevied);
			}
			catch {
				return null;
			}

			if (baseRequest == null || Helpers.IsNullOrEmpty(baseRequest.RequestType) || Helpers.IsNullOrEmpty(baseRequest.RequestObject)) {
				return null;
			}

			switch (baseRequest.RequestType) {
				case nameof(GpioRequest):
					return await OnGpioRequestAsync(ObjectFromString<GpioRequest>(baseRequest.RequestObject)).ConfigureAwait(false);
				default:
					return null;
			}
		}

		private async Task<string> OnGpioRequestAsync(GpioRequest request) {
			if (request == null || Helpers.IsNullOrEmpty(request.Command)) {
				return null;
			}

			await ProcessingSemaphore.WaitAsync().ConfigureAwait(false);

			int pinNumber;
			GpioPinDriveMode mode;
			GpioPinValue value;
			string result = null;

			switch (request.Command) {
				case "SETGPIO" when request.StringParameters.Count == 3:
					if (request.StringParameters == null) {
						result = null;
					}

					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					mode = (GpioPinDriveMode) Convert.ToInt32(request.StringParameters[1].Trim());
					value = (GpioPinValue) Convert.ToInt32(request.StringParameters[2].Trim());
					if (Core.Controller.SetGPIO(pinNumber, mode, value)) {
						result = $"Successfully set {pinNumber} pin to {mode.ToString()} mode with value {value.ToString()}";
					}
					else {
						result = "Failed";
					}
					break;

				case "SETGPIO" when request.StringParameters.Count == 2:
					if (request.StringParameters == null) {
						result = null;
					}

					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					mode = (GpioPinDriveMode) Convert.ToInt32(request.StringParameters[1].Trim());

					if (Core.Controller.SetGPIO(pinNumber, mode, GpioPinValue.High)) {
						result = $"Successfully set {pinNumber} pin to {mode.ToString()} mode.";
					}
					else {
						result = "Failed";
					}
					break;

				case "GETGPIO" when request.StringParameters.Count == 1:
					if (request.StringParameters == null) {
						result = null;
					}

					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					GpioPinConfig pinConfig = Core.Controller.FetchPinStatus(pinNumber);

					GetGpioResponse response = new GetGpioResponse() {
						DriveMode = pinConfig.Mode == Enums.PinMode.Input ? GpioPinDriveMode.Input : GpioPinDriveMode.Output,
						PinNumber = pinConfig.Pin,
						PinValue = pinConfig.IsOn ? GpioPinValue.Low : GpioPinValue.High
					};

					result = ObjectToString<GetGpioResponse>(response);
					break;
			}

			ProcessingSemaphore.Release();
			return result;
		}

		public void Dispose() {
			if (Listener != null) {
				StopServer = true;
				Listener.Stop();
			}
		}

		public static string ObjectToString<T>(T obj) => JsonConvert.SerializeObject(obj);

		public static T ObjectFromString<T>(string obj) => JsonConvert.DeserializeObject<T>(obj);
	}
}

