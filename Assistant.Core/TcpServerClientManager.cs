using Assistant.Gpio;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Server.CoreServer;
using Assistant.Server.CoreServer.EventArgs;
using Assistant.Server.CoreServer.Requests;
using Assistant.Server.CoreServer.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Assistant.Gpio.PiController;
using static Assistant.Logging.Enums;
using static Assistant.Server.CoreServer.CoreServerEnums;

namespace Assistant.Core {
	public class TcpServerClientManager : IDisposable {
		private readonly ILogger Logger;
		public Connection? Client { get; private set; }
		private readonly string ClientUid = string.Empty;

		public TcpServerClientManager(string uid) {
			ClientUid = uid;
			Logger = new Logger($"{typeof(TcpServerClientManager).Name} | {ClientUid}");
		}

		public TcpServerClientManager Handle() {
			if (string.IsNullOrEmpty(ClientUid)) {
				return this;
			}

			Connection? client = GetConnection(ClientUid);

			if (client == null || client.IsDisposed) {
				return this;
			}

			Client = client;
			Client.Received += Client_Received;
			Client.Connected += ClientOnConnected;
			Client.Disconnected += ClientOnDisconnected;
			return this;
		}

		private void ClientOnDisconnected(object sender, OnDisconnectedEventArgs e) {

		}

		private void ClientOnConnected(object sender, OnConnectedEventArgs e) {

		}

		private async void Client_Received(object sender, OnReceivedEventArgs e) {
			if (e == null || e.BaseRequest == null || string.IsNullOrEmpty(e.ReceivedRaw)) {
				return;
			}

			Logger.Log($"Received command from -> {e.ReceivedFromAddress} ({e.BaseRequest.TypeCode.ToString()})");

			await ProcessOnReceived(e.BaseRequest).ConfigureAwait(false);
		}

		private async Task ProcessOnReceived(BaseRequest request) {
			switch (request.TypeCode) {
				case TYPE_CODE.EVENT_PIN_STATE:
					break;
				case TYPE_CODE.UNKNOWN:
					break;
				case TYPE_CODE.SET_ALARM:
					break;
				case TYPE_CODE.SET_GPIO:
					if (string.IsNullOrEmpty(request.RequestObject)) {
						return;
					}

					SetGpioRequest setGpioRequest = JsonConvert.DeserializeObject<SetGpioRequest>(request.RequestObject);
					if (!PiController.IsValidPin(setGpioRequest.PinNumber)) {
						return;
					}

					if (Core.PiController == null || !Core.PiController.IsControllerProperlyInitialized || Core.PinController == null) {
						return;
					}

					if (Client == null || Client.IsDisposed) {
						return;
					}

					if (Core.PinController.SetGpioValue(setGpioRequest.PinNumber, (GpioPinMode) setGpioRequest.PinMode, (GpioPinState) setGpioRequest.PinState)) {
						GpioPinConfig config = Core.PinController.GetGpioConfig(setGpioRequest.PinNumber);
						if (await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO, "Success!", GpioPinConfig.AsJson(config))).ConfigureAwait(false)) {
							Logger.Log($"{request.TypeCode.ToString()} response send!", LogLevels.Trace);
							return;
						}
					}

					await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO, "Failed.", string.Empty)).ConfigureAwait(false);
					break;
				case TYPE_CODE.SET_GPIO_DELAYED:
					if (string.IsNullOrEmpty(request.RequestObject)) {
						return;
					}

					SetGpioDelayedRequest setGpioDelayedRequest = JsonConvert.DeserializeObject<SetGpioDelayedRequest>(request.RequestObject);
					if (!PiController.IsValidPin(setGpioDelayedRequest.PinNumber)) {
						return;
					}

					if (Core.PiController == null || !Core.PiController.IsControllerProperlyInitialized || Core.PinController == null) {
						return;
					}

					if (Client == null || Client.IsDisposed) {
						return;
					}

					if (Core.PinController.SetGpioWithTimeout(setGpioDelayedRequest.PinNumber, (GpioPinMode) setGpioDelayedRequest.PinMode, (GpioPinState) setGpioDelayedRequest.PinState, TimeSpan.FromMinutes(setGpioDelayedRequest.Delay))) {
						GpioPinConfig config = Core.PinController.GetGpioConfig(setGpioDelayedRequest.PinNumber);
						if (await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO_DELAYED, "Success!", GpioPinConfig.AsJson(config))).ConfigureAwait(false)) {
							Logger.Log($"{request.TypeCode.ToString()} response send!", LogLevels.Trace);
							return;
						}
					}

					await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO_DELAYED, "Failed.", string.Empty)).ConfigureAwait(false);
					break;
				case TYPE_CODE.SET_REMAINDER:
					break;
				case TYPE_CODE.GET_GPIO:
					break;
				case TYPE_CODE.GET_GPIO_PIN:
					break;
				case TYPE_CODE.GET_WEATHER:
					break;
				case TYPE_CODE.GET_ASSISTANT_INFO:
					break;
				case TYPE_CODE.GET_PI_INFO:
					break;
				case TYPE_CODE.SET_PI:
					break;
				case TYPE_CODE.SET_ASSISTANT:
					break;
				default:
					break;
			}
		}

		private static Connection? GetConnection(string uid) {
			if (string.IsNullOrEmpty(uid)) {
				return null;
			}

			if (CoreServerBase.ConnectedClientsCount <= 0) {
				return null;
			}

			foreach (KeyValuePair<string, Connection> values in CoreServerBase.ConnectedClients) {
				if ((string.IsNullOrEmpty(values.Key)) || (values.Value == null)) {
					continue;
				}

				if (!values.Key.Equals(uid, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				return values.Value;
			}

			return null;
		}

		public void Dispose() => Client?.Dispose();
	}
}
