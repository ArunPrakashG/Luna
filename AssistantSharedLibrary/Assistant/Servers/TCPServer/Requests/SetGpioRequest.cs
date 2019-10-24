using Newtonsoft.Json;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.Requests {
	public class SetGpioRequest {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public Enums.GpioPinMode PinMode { get; set; }

		[JsonProperty]
		public Enums.GpioPinState PinState { get; set; }

		public SetGpioRequest(int pinNumber, Enums.GpioPinMode pinMode, Enums.GpioPinState pinState) {
			PinNumber = pinNumber;
			PinMode = pinMode;
			PinState = pinState;
		}
	}
}
