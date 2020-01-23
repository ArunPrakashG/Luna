using Newtonsoft.Json;
using System;

namespace Assistant.PushBullet.Responses.Chats {
	[Serializable]
	public class ChatsBase {
		[JsonProperty("active")]
		public bool IsActive { get; set; }

		[JsonProperty("iden")]
		public string Iden { get; set; }

		[JsonProperty("created")]
		public float Created { get; set; }

		[JsonProperty("modified")]
		public float Modified { get; set; }

		[JsonProperty("with")]
		public With WithObject { get; set; }

		[Serializable]
		public class With {
			[JsonProperty("type")]
			public string Type { get; set; }

			[JsonProperty("iden")]
			public string Iden { get; set; }

			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("email")]
			public string Email { get; set; }

			[JsonProperty("email_normalized")]
			public string EmailNormalized { get; set; }

			[JsonProperty("image_url")]
			public string ImageUrl { get; set; }
		}
	}
}
