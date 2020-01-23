using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBullet.Responses.User {
	public class UserBase {
		[JsonProperty("created")]
		public float Created { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("email_normalized")]
		public string EmailNormalized { get; set; }

		[JsonProperty("iden")]
		public string Iden { get; set; }

		[JsonProperty("image_url")]
		public string ImageUrl { get; set; }

		[JsonProperty("max_upload_size")]
		public float MaxUploadSize { get; set; }

		[JsonProperty("modified")]
		public float Modified { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}
}
