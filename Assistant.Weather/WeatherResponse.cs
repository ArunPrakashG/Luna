using Newtonsoft.Json;
using System;

namespace Assistant.Weather {
	[Serializable]
	public class WeatherResponse {
		[JsonProperty("coord")]
		public Coordinates? Location { get; set; }

		[JsonProperty("weather")]
		public WeatherCollection[]? Weather { get; set; }

		[JsonProperty("main")]
		public Main? Data { get; set; }

		[JsonProperty("wind")]
		public WindData? Wind { get; set; }

		[JsonProperty("clouds")]
		public CloudsData? Clouds { get; set; }

		[JsonProperty("dt")]
		public int DateTime { get; set; }

		[JsonProperty("sys")]
		public Sys? CountrySys { get; set; }

		[JsonProperty("timezone")]
		public int Timezone { get; set; }

		[JsonProperty("name")]
		public string? LocationName { get; set; }

		[Serializable]
		public class Coordinates {
			[JsonProperty("lon")]
			public float Longitude { get; set; }

			[JsonProperty("lat")]
			public float Latitude { get; set; }
		}

		[Serializable]
		public class Main {
			[JsonProperty("temp")]
			public float Temperature { get; set; }
			[JsonProperty("feels_like")]
			public float FeelsLike { get; set; }
			[JsonProperty("temp_min")]
			public float TemperatureMin { get; set; }
			[JsonProperty("temp_max")]
			public float TemperatureMax { get; set; }
			[JsonProperty("pressure")]
			public int Pressure { get; set; }
			[JsonProperty("humidity")]
			public int Humidity { get; set; }
			[JsonProperty("sea_level")]
			public int SeaLevel { get; set; }
			[JsonProperty("grnd_level")]
			public int GroundLevel { get; set; }
		}

		[Serializable]
		public class WindData {
			[JsonProperty("speed")]
			public float Speed { get; set; }

			[JsonProperty("deg")]
			public int Degree { get; set; }
		}

		[Serializable]
		public class CloudsData {
			[JsonProperty("all")]
			public int All { get; set; }
		}

		[Serializable]
		public class Sys {
			[JsonProperty("country")]
			public string? CountryCode { get; set; }
			[JsonProperty("sunrise")]
			public int Sunrise { get; set; }
			[JsonProperty("sunset")]
			public int Sunset { get; set; }
		}

		[Serializable]
		public class WeatherCollection {
			[JsonProperty("id")]
			public int Id { get; set; }
			[JsonProperty("main")]
			public string? Main { get; set; }
			[JsonProperty("description")]
			public string? Description { get; set; }
			[JsonProperty("icon")]
			public string? Icon { get; set; }
		}
	}
}
