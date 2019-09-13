using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class WeatherRequest {
		[JsonProperty]
		public string LocationPinCode { get; set; }

		[JsonProperty]
		public string CountryCode { get; set; } = "in";
	}
}
