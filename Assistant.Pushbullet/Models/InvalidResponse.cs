using Newtonsoft.Json;
using System;

namespace Assistant.Pushbullet.Models {
	[Serializable]
	public class InvalidResponse {
		[JsonProperty("error")]
		public Error ErrorObject { get; set; } = new Error();

		public class Error {
			[JsonProperty("cat")]
			public string? Cat { get; set; }
			[JsonProperty("message")]
			public string? Message { get; set; }
			[JsonProperty("type")]
			public string? Type { get; set; }
		}
	}
}
