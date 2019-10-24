using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Servers.SecureLine.Requests;
using Assistant.Servers.SecureLine.Responses;
using Assistant.Weather;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Servers.SecureLine {
	public class ConnectedClient {
		public string IPAddress { get; set; } = string.Empty;
		public int ConnectedCount { get; set; }
		public DateTime LastConnectedTime { get; set; }
	}

	public class SecureLineServer : IDisposable {
		private TcpListener? Listener { get; set; }
		private static int ServerPort { get; set; }
		private static IPAddress ListerningAddress { get; set; } = IPAddress.Any;
		private static IPEndPoint? EndPoint { get; set; }
		public static bool IsOnline { get; private set; }
		public static bool StopServer { get; set; }
		private readonly Logger Logger = new Logger("SECURE-LINE");

		public static List<ConnectedClient> ConnectedClients = new List<ConnectedClient>();
		private static readonly SemaphoreSlim ProcessingSemaphore = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim ServerSemaphore = new SemaphoreSlim(1, 1);

		public SecureLineServer InitSecureLine(IPAddress address, int port = 1111) {
			ListerningAddress = address ?? throw new ArgumentNullException(nameof(address));
			ServerPort = port <= 0 ? throw new ArgumentNullException(nameof(port)) : port;
			EndPoint = new IPEndPoint(ListerningAddress, ServerPort);
			return this;
		}

		public bool StartSecureLine() {
			Logger.Log("Starting secure line server...", Enums.LogLevels.Trace);
			Listener = new TcpListener(EndPoint);
			Listener.Start(10);
			IsOnline = true;

			Helpers.InBackgroundThread(async () => {
				try {
					while (!StopServer && Listener != null) {
						await ServerSemaphore.WaitAsync().ConfigureAwait(false);
						Socket socket = await Listener.AcceptSocketAsync().ConfigureAwait(false);
						byte[] receiveBuffer = new byte[5024];
						int b = socket.Receive(receiveBuffer);
						EndPoint remoteEndpoint = socket.RemoteEndPoint;

						ConnectedClient client = new ConnectedClient() {
							IPAddress = remoteEndpoint.ToString()?.Split(':')[0].Trim() ?? string.Empty,
							ConnectedCount = 0,
							LastConnectedTime = DateTime.Now
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
						ServerSemaphore.Release();
					}
				}
				catch (Exception e) {
					Logger.Log(e);
				}
				finally {
					Listener.Stop();
				}
			}, "Secure Line Server", true);

			Logger.Log($"Secure line server running at {ServerPort} port", Enums.LogLevels.Trace);
			return true;
		}

		private async Task<string> OnReceviedAsync(string recevied) {
			if (Helpers.IsNullOrEmpty(recevied)) {
				return string.Empty;
			}

			Logger.Log(recevied, Enums.LogLevels.Trace);
			BaseRequest baseRequest;
			try {
				baseRequest = ObjectFromString<BaseRequest>(recevied);
			}
			catch {
				return string.Empty;
			}

			if (baseRequest == null || Helpers.IsNullOrEmpty(baseRequest.RequestType) || Helpers.IsNullOrEmpty(baseRequest.RequestObject)) {
				return string.Empty;
			}

			switch (baseRequest.RequestType) {
				case "GpioRequest":
					return await OnGpioRequestAsync(ObjectFromString<GpioRequest>(baseRequest.RequestObject)).ConfigureAwait(false);
				case "WeatherRequest":
					return await OnWeatherRequestAsync(ObjectFromString<WeatherRequest>(baseRequest.RequestObject)).ConfigureAwait(false);
				case "RemainderRequest":
					return await OnRemainderRequestAsync(ObjectFromString<RemainderRequest>(baseRequest.RequestObject)).ConfigureAwait(false);
				case "AlarmRequest":
					return await OnAlarmRequestAsync(ObjectFromString<AlarmRequest>(baseRequest.RequestObject)).ConfigureAwait(false);
				default:
					return string.Empty;
			}
		}

		private async Task<string> OnAlarmRequestAsync(AlarmRequest alarmRequest) {
			if (alarmRequest == null || Helpers.IsNullOrEmpty(alarmRequest.AlarmMessage) || alarmRequest.HoursFromNow <= 0) {
				return "Alarm parameters are empty.";
			}

			try {
				await ProcessingSemaphore.WaitAsync().ConfigureAwait(false);
				if (Core.AlarmManager.SetAlarm(alarmRequest.HoursFromNow, alarmRequest.AlarmMessage, alarmRequest.UseTTS, TimeSpan.FromHours(alarmRequest.RepeatHours), alarmRequest.Repeat)) {
					return $"Successfully set an alarm at {alarmRequest.HoursFromNow} hours from now.";
				}

				return "Failed to set alarm.";
			}
			finally {
				ProcessingSemaphore.Release();
			}
		}

		private async Task<string> OnRemainderRequestAsync(RemainderRequest remainderRequest) {
			if (remainderRequest == null || Helpers.IsNullOrEmpty(remainderRequest.Message) || remainderRequest.MinutesUntilRemainding <= 0) {
				return "Message or the minutes specified is invalid.";
			}

			await ProcessingSemaphore.WaitAsync().ConfigureAwait(false);
			if (Core.RemainderManager.Remind(remainderRequest.Message, remainderRequest.MinutesUntilRemainding)) {
				ProcessingSemaphore.Release();
				return "Successfully set remainder for " + remainderRequest.Message;
			}

			ProcessingSemaphore.Release();
			return "Failed to set remainder.";
		}

		private async Task<string> OnWeatherRequestAsync(WeatherRequest request) {
			if (request == null || Helpers.IsNullOrEmpty(request.LocationPinCode) || Helpers.IsNullOrEmpty(request.CountryCode)) {
				return "The request is in incorrect format.";
			}

			if (!int.TryParse(request.LocationPinCode, out int pinCode)) {
				return "Could not parse the specified pin code.";
			}

			if (Core.Config.OpenWeatherApiKey == null || Core.Config.OpenWeatherApiKey.IsNull()) {
				return "The api key is null";
			}

			await ProcessingSemaphore.WaitAsync().ConfigureAwait(false);
			(bool status, WeatherData response) = Core.WeatherApi.GetWeatherInfo(Core.Config.OpenWeatherApiKey, pinCode, request.CountryCode);

			if (!status || response == null) {
				ProcessingSemaphore.Release();
				return "Internal error occured during the process.";
			}

			ProcessingSemaphore.Release();
			return ObjectToString<WeatherData>(response);
		}

		private async Task<string> OnGpioRequestAsync(GpioRequest request) {
			if (request == null || Helpers.IsNullOrEmpty(request.Command)) {
				return string.Empty;
			}

			await ProcessingSemaphore.WaitAsync().ConfigureAwait(false);

			int pinNumber;
			Enums.GpioPinMode mode;
			Enums.GpioPinState value;
			string result = string.Empty;

			if (Core.PiController == null) {
				return "The PiController is malfunctioning.";
			}

			switch (request.Command) {
				case "SETGPIO" when request.StringParameters.Count == 4:
					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					mode = (Enums.GpioPinMode) Convert.ToInt32(request.StringParameters[1].Trim());
					value = (Enums.GpioPinState) Convert.ToInt32(request.StringParameters[2].Trim());
					int delay = Convert.ToInt32(request.StringParameters[3].Trim());
					result = Core.PiController.GetPinController().SetGpioWithTimeout(pinNumber, mode, value, TimeSpan.FromMinutes(delay))
						? $"Successfully set {pinNumber} pin to {mode.ToString()} mode with value {value.ToString()} for {delay} minutes."
						: "Failed";
					break;
				case "SETGPIO" when request.StringParameters.Count == 3:
					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					mode = (Enums.GpioPinMode) Convert.ToInt32(request.StringParameters[1].Trim());
					value = (Enums.GpioPinState) Convert.ToInt32(request.StringParameters[2].Trim());
					result = Core.PiController.GetPinController().SetGpioValue(pinNumber, mode, value)
						? $"Successfully set {pinNumber} pin to {mode.ToString()} mode with value {value.ToString()}"
						: "Failed";
					break;

				case "SETGPIO" when request.StringParameters.Count == 2:
					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					mode = (Enums.GpioPinMode) Convert.ToInt32(request.StringParameters[1].Trim());

					result = Core.PiController.GetPinController().SetGpioValue(pinNumber, mode)
						? $"Successfully set {pinNumber} pin to {mode.ToString()} mode."
						: "Failed";
					break;

				case "GETGPIO" when request.StringParameters.Count == 1:
					pinNumber = Convert.ToInt32(request.StringParameters[0].Trim());
					GpioPinConfig pinConfig = Core.PiController.GetPinController().GetGpioConfig(pinNumber);

					GetGpioResponse response = new GetGpioResponse() {
						DriveMode = pinConfig.Mode,
						PinNumber = pinConfig.Pin,
						PinValue = pinConfig.PinValue
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

		private static bool IsNullForRange(IEnumerable<string> range) {
			if (range == null || range.Count() <= 0) {
				return true;
			}

			foreach (string s in range) {
				if (s == null || s.IsNull() || Helpers.IsNullOrEmpty(s)) {
					return true;
				}
			}

			return false;
		}
	}
}

