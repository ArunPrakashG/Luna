using Newtonsoft.Json;
using static Assistant.Server.CoreServer.CoreServerEnums;

namespace Assistant.Server.CoreServer.Requests {
	public class SetGpioRequest {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public GPIO_PIN_MODE PinMode { get; set; }

		[JsonProperty]
		public GPIO_PIN_STATE PinState { get; set; }

		public SetGpioRequest(int pinNumber, GPIO_PIN_MODE pinMode, GPIO_PIN_STATE pinState) {
			PinNumber = pinNumber;
			PinMode = pinMode;
			PinState = pinState;
		}
	}
}
