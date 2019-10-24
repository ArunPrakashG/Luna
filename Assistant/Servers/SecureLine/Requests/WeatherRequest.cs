using Newtonsoft.Json;

namespace Assistant.Servers.SecureLine.Requests {
	public class WeatherRequest {
		[JsonProperty]
		public string LocationPinCode { get; set; } = string.Empty;

		[JsonProperty]
		public string CountryCode { get; set; } = "in";
	}
}
