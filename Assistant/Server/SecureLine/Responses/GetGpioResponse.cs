using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.Server.SecureLine.Responses {
	public class GetGpioResponse {
		[JsonProperty]
		public int PinNumber { get; set; }

		[JsonProperty]
		public GpioPinDriveMode DriveMode { get; set; }

		[JsonProperty]
		public GpioPinValue PinValue { get; set; }
	}
}
