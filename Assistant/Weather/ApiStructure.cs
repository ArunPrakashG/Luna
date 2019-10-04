namespace Assistant.Weather {
	public class ApiResponseStructure {

		public class Rootobject {
			public Coord? coord { get; set; }
			public Weather[]? weather { get; set; }
			public string _base { get; set; } = string.Empty;
			public Main? main { get; set; }
			public Wind? wind { get; set; }
			public Clouds? clouds { get; set; }
			public int dt { get; set; }
			public Sys? sys { get; set; }
			public int timezone { get; set; }
			public int id { get; set; }
			public string name { get; set; } = string.Empty;
			public int cod { get; set; }
		}

		public class Coord {
			public float lon { get; set; }
			public float lat { get; set; }
		}

		public class Main {
			public float temp { get; set; }
			public float pressure { get; set; }
			public int humidity { get; set; }
			public float temp_min { get; set; }
			public float temp_max { get; set; }
			public float sea_level { get; set; }
			public float grnd_level { get; set; }
		}

		public class Wind {
			public float speed { get; set; }
			public float deg { get; set; }
		}

		public class Clouds {
			public int all { get; set; }
		}

		public class Sys {
			public float message { get; set; }
			public string country { get; set; } = string.Empty;
			public int sunrise { get; set; }
			public int sunset { get; set; }
		}

		public class Weather {
			public int id { get; set; }
			public string main { get; set; } = string.Empty;
			public string description { get; set; } = string.Empty;
			public string icon { get; set; } = string.Empty;
		}

	}
}
