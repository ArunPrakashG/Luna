using Assistant.AssistantCore;
using Newtonsoft.Json;

namespace Assistant.Server.TCPServer.Commands {
	public class SetGpioDelayed {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public Enums.GpioPinMode PinMode { get; set; }

		[JsonProperty]
		public Enums.GpioPinState PinState { get; set; }

		[JsonProperty]
		public int Delay { get; set; }

		public SetGpioDelayed(int pinNumber, Enums.GpioPinMode pinMode, Enums.GpioPinState pinState, int delay) {
			PinNumber = pinNumber;
			PinMode = pinMode;
			PinState = pinState;
			Delay = delay;
		}

		public string AsJson() => JsonConvert.SerializeObject(this);
	}
}
