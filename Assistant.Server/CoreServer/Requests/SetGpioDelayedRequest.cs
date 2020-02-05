using Newtonsoft.Json;
using static Assistant.Server.CoreServer.CoreServerEnums;

namespace Assistant.Server.CoreServer.Requests {
	public class SetGpioDelayedRequest {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public GPIO_PIN_MODE PinMode { get; set; }

		[JsonProperty]
		public GPIO_PIN_STATE PinState { get; set; }

		[JsonProperty]
		public int Delay { get; set; }

		public SetGpioDelayedRequest(int pinNumber, GPIO_PIN_MODE pinMode, GPIO_PIN_STATE pinState, int delay) {
			PinNumber = pinNumber;
			PinMode = pinMode;
			PinState = pinState;
			Delay = delay;
		}
	}
}
