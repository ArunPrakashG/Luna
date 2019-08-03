using Newtonsoft.Json;

namespace Assistant.PushBullet.ApiResponse {
	public class ChannelInfoResponse {
		[JsonProperty("iden")]
		public string Identifier { get; set; }
		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("tag")]
		public string Tag { get; set; }
		[JsonProperty("subscriber_count")]
		public int SubscriberCount { get; set; }
		[JsonProperty("recent_pushes")]
		public Recent_Pushes[] RecentPushes { get; set; }

		public class Recent_Pushes {
			[JsonProperty("active")]
			public bool IsActive { get; set; }
			[JsonProperty("created")]
			public float CreatedAt { get; set; }
			[JsonProperty("modified")]
			public float ModifiedAt { get; set; }
			[JsonProperty("type")]
			public string Type { get; set; }
			[JsonProperty("dismissed")]
			public bool Dismissed { get; set; }
			[JsonProperty("guid")]
			public string Guid { get; set; }
			[JsonProperty("direction")]
			public string Direction { get; set; }
			[JsonProperty("sender_name")]
			public string SenderName { get; set; }
			[JsonProperty("channel_iden")]
			public string ChannelIdentifier { get; set; }
			[JsonProperty("body")]
			public string Body { get; set; }
		}
	}
}
