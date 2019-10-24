using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.Requests {
	public class SetGpioDelayedRequest {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public Enums.GpioPinMode PinMode { get; set; }

		[JsonProperty]
		public Enums.GpioPinState PinState { get; set; }

		[JsonProperty]
		public int Delay { get; set; }

		public SetGpioDelayedRequest(int pinNumber, Enums.GpioPinMode pinMode, Enums.GpioPinState pinState, int delay) {
			PinNumber = pinNumber;
			PinMode = pinMode;
			PinState = pinState;
			Delay = delay;
		}
	}
}
