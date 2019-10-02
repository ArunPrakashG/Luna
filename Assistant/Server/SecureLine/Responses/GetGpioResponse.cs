using Assistant.AssistantCore;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Responses {
	public class GetGpioResponse {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public Enums.GpioPinMode DriveMode { get; set; }

		[JsonProperty]
		public Enums.GpioPinState PinValue { get; set; }
	}
}
