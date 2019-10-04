using Newtonsoft.Json;

namespace Assistant.PushBullet.ApiResponse {
	public class ListSubscriptionsResponse {

		[JsonProperty("accounts")]
		public object[]? Accounts { get; set; }
		[JsonProperty("blocks")]
		public object[]? Blocks { get; set; }
		[JsonProperty("channels")]
		public object[]? Channels { get; set; }
		[JsonProperty("chats")]
		public object[]? Chats { get; set; }
		[JsonProperty("clients")]
		public object[]? Clients { get; set; }
		[JsonProperty("contacts")]
		public object[]? Contacts { get; set; }
		[JsonProperty("devices")]
		public object[]? Devices { get; set; }
		[JsonProperty("grants")]
		public object[]? Grants { get; set; }
		[JsonProperty("pushes")]
		public object[]? Pushes { get; set; }
		[JsonProperty("profiles")]
		public object[]? Profiles { get; set; }
		[JsonProperty("subscriptions")]
		public Subscription[]? Subscriptions { get; set; }
		[JsonProperty("texts")]
		public object[]? Texts { get; set; }

		public class Subscription {
			[JsonProperty("active")]
			public bool IsActive { get; set; }
			[JsonProperty("iden")]
			public string Identifier { get; set; } = string.Empty;
			[JsonProperty("created")]
			public float CreatedAt { get; set; }
			[JsonProperty("modified")]
			public float ModifiedAt { get; set; }
			[JsonProperty("channel")]
			public Channel? Channel { get; set; }
		}

		public class Channel {
			[JsonProperty("iden")]
			public string Identifier { get; set; } = string.Empty;
			[JsonProperty("tag")]
			public string Tag { get; set; } = string.Empty;
			[JsonProperty("name")]
			public string Name { get; set; } = string.Empty;
		}
	}
}
