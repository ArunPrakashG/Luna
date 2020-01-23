using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBullet.Responses {
	[Serializable]
	public class InvalidResponse {

		[JsonProperty("error")]
		public Error? ErrorObject { get; set; }

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
