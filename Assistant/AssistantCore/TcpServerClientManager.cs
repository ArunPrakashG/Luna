using Assistant.AssistantCore.PiGpio;
using Assistant.Log;
using SharedLibrary.TCPServer;
using SharedLibrary.TCPServer.EventArgs;
using SharedLibrary.TCPServer.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assistant.AssistantCore {
	public class TcpServerClientManager : IDisposable {
		private readonly Logger Logger;
		public Connection? Client { get; private set; }
		private readonly string ClientUid = string.Empty;

		public TcpServerClientManager(string uid) {
			ClientUid = uid;
			Logger = new Logger($"CLIENT | {ClientUid}");
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
			client.Received += ClientOnRecevied;
			client.Connected += ClientOnConnected;
			client.Disconnected += ClientOnDisconnected;
			return this;
		}

		private void ClientOnDisconnected(object sender, OnDisconnectedEventArgs e) {

		}

		private void ClientOnConnected(object sender, OnConnectedEventArgs e) {

		}

		private async void ClientOnRecevied(object sender, OnReceivedEventArgs e) {
			if (e == null || e.ReceivedObject == null || string.IsNullOrEmpty(e.ReceivedObject)) {
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

					SetGpioRequest setGpioRequest = BaseRequest.DeserializeRequest<SetGpioRequest>(request.RequestObject);
					if (!PiController.IsValidPin(setGpioRequest.PinNumber)) {
						return;
					}

					if (Core.PiController == null || !Core.PiController.IsControllerProperlyInitialized) {
						return;
					}

					if (Client == null || Client.IsDisposed) {
						return;
					}

					if (Core.PiController.GetPinController().SetGpioValue(setGpioRequest.PinNumber, (Enums.GpioPinMode) setGpioRequest.PinMode, (Enums.GpioPinState) setGpioRequest.PinState)) {
						GpioPinConfig config = Core.PiController.GetPinController().GetGpioConfig(setGpioRequest.PinNumber);
						if (await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO, "Success!", GpioPinConfig.AsJson(config))).ConfigureAwait(false)) {
							Logger.Log($"{request.TypeCode.ToString()} response send!", Enums.LogLevels.Trace);
							return;
						}
					}

					await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO, "Failed.", string.Empty)).ConfigureAwait(false);
					break;
				case TYPE_CODE.SET_GPIO_DELAYED:
					if (string.IsNullOrEmpty(request.RequestObject)) {
						return;
					}

					SetGpioDelayedRequest setGpioDelayedRequest = BaseRequest.DeserializeRequest<SetGpioDelayedRequest>(request.RequestObject);
					if (!PiController.IsValidPin(setGpioDelayedRequest.PinNumber)) {
						return;
					}

					if (Core.PiController == null || !Core.PiController.IsControllerProperlyInitialized) {
						return;
					}

					if (Client == null || Client.IsDisposed) {
						return;
					}

					if (Core.PiController.GetPinController().SetGpioWithTimeout(setGpioDelayedRequest.PinNumber, (Enums.GpioPinMode) setGpioDelayedRequest.PinMode, (Enums.GpioPinState) setGpioDelayedRequest.PinState, TimeSpan.FromMinutes(setGpioDelayedRequest.Delay))) {
						GpioPinConfig config = Core.PiController.GetPinController().GetGpioConfig(setGpioDelayedRequest.PinNumber);
						if (await Client.SendAsync(new BaseResponse(DateTime.Now, TYPE_CODE.SET_GPIO_DELAYED, "Success!", GpioPinConfig.AsJson(config))).ConfigureAwait(false)) {
							Logger.Log($"{request.TypeCode.ToString()} response send!", Enums.LogLevels.Trace);
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

			if (ServerBase.ConnectedClientsCount <= 0) {
				return null;
			}

			foreach (KeyValuePair<string, Connection> values in ServerBase.ConnectedClients) {
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
