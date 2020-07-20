using Newtonsoft.Json;
using System;

namespace Luna.Pushbullet.Models {
	[Serializable]
	public class Chat {
		[JsonProperty("active")]
		public bool IsActive { get; set; }

		[JsonProperty("iden")]
		public string? Iden { get; set; }

		[JsonProperty("created")]
		public float Created { get; set; }

		[JsonProperty("modified")]
		public float Modified { get; set; }

		[JsonProperty("with")]
		public WithObject With { get; set; } = new WithObject();

		[Serializable]
		public class WithObject {
			[JsonProperty("type")]
			public string? Type { get; set; }

			[JsonProperty("iden")]
			public string? Iden { get; set; }

			[JsonProperty("name")]
			public string? Name { get; set; }

			[JsonProperty("email")]
			public string? Email { get; set; }

			[JsonProperty("email_normalized")]
			public string? EmailNormalized { get; set; }

			[JsonProperty("image_url")]
			public string? ImageUrl { get; set; }
		}
	}
}
