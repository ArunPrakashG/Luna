using Newtonsoft.Json;
using System;

namespace Assistant.Pushbullet.Models {
	[Serializable]
	public class Subscription {
		[JsonProperty("active")]
		public bool Active { get; set; }

		[JsonProperty("Channel")]
		public ChannelObject Channel { get; set; } = new ChannelObject();

		[JsonProperty("created")]
		public float Created { get; set; }

		[JsonProperty("iden")]
		public string? Iden { get; set; }

		[JsonProperty("modified")]
		public float Modified { get; set; }

		[Serializable]
		public class ChannelObject {
			[JsonProperty("description")]
			public string? Description { get; set; }

			[JsonProperty("iden")]
			public string? Iden { get; set; }

			[JsonProperty("image_url")]
			public string? ImageUrl { get; set; }

			[JsonProperty("name")]
			public string? Name { get; set; }

			[JsonProperty("tag")]
			public string? Tag { get; set; }
		}
	}
}
