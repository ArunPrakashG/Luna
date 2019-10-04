using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class WeatherRequest {
		[JsonProperty]
		public string LocationPinCode { get; set; } = string.Empty;

		[JsonProperty]
		public string CountryCode { get; set; } = "in";
	}
}
