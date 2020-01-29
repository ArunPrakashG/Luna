using Assistant.AssistantCore;
using Newtonsoft.Json;

namespace Assistant.Servers.TCPServer.Commands {
	public class SetGpio {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public Enums.GpioPinMode PinMode { get; set; }

		[JsonProperty]
		public Enums.GpioPinState PinState { get; set; }

		public SetGpio(int pinNumber, Enums.GpioPinMode pinMode, Enums.GpioPinState pinState) {
			PinNumber = pinNumber;
			PinMode = pinMode;
			PinState = pinState;
		}

		public string AsJson() => JsonConvert.SerializeObject(this);
	}
}
