using Newtonsoft.Json;

namespace Assistant.Weather {

	public class WeatherData {

		[JsonProperty]
		public float Logitude { get; set; }

		[JsonProperty]
		public float Latitude { get; set; }

		[JsonProperty]
		public string WeatherMain { get; set; }

		[JsonProperty]
		public string WeatherDescription { get; set; }

		[JsonProperty]
		public string WeatherIcon { get; set; }

		[JsonProperty]
		public float Temperature { get; set; }

		[JsonProperty]
		public float Pressure { get; set; }

		[JsonProperty]
		public float Humidity { get; set; }

		[JsonProperty]
		public float SeaLevel { get; set; }

		[JsonProperty]
		public float GroundLevel { get; set; }

		[JsonProperty]
		public float WindSpeed { get; set; }

		[JsonProperty]
		public float WindDegree { get; set; }

		[JsonProperty]
		public float Clouds { get; set; }

		[JsonProperty]
		public long TimeZone { get; set; }

		[JsonProperty]
		public string LocationName { get; set; }
	}
}
