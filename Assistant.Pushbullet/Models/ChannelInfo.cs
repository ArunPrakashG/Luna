using Newtonsoft.Json;

namespace Assistant.Pushbullet.Models {
	public class ChannelInfo {
		[JsonProperty("active")]
		public bool Active { get; set; }

		[JsonProperty("created")]
		public float Created { get; set; }

		[JsonProperty("description")]
		public string? Description { get; set; }

		[JsonProperty("iden")]
		public string? Iden { get; set; }

		[JsonProperty("image_url")]
		public string? ImageUrl { get; set; }

		[JsonProperty("modified")]
		public float Modified { get; set; }

		[JsonProperty("name")]
		public string? Name { get; set; }

		[JsonProperty("subscriber_count")]
		public float SubscriberCount { get; set; }

		[JsonProperty("tag")]
		public string? Tag { get; set; }
	}
}
