using Newtonsoft.Json;
using System;

namespace Assistant.Pushbullet.Models {
	[Serializable]
	public class Response {
		[JsonIgnore]
		public bool IsDeleteRequestSuccess { get; set; }

		[JsonProperty("accounts", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Accounts { get; set; }

		[JsonProperty("blocks", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Blocks { get; set; }

		[JsonProperty("channels", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Channels { get; set; }

		[JsonProperty("chats", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public Chat[] Chats { get; set; } = new Chat[] { };

		[JsonProperty("clients", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Clients { get; set; }

		[JsonProperty("contacts", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Contacts { get; set; }

		[JsonProperty("devices", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public Device[] Devices { get; set; } = new Device[] { };

		[JsonProperty("grants", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Grants { get; set; }

		[JsonProperty("pushes", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public Push[] Pushes { get; set; } = new Push[] { };

		[JsonProperty("profiles", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Profiles { get; set; }

		[JsonProperty("subscriptions", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public Subscription[] Subscriptions { get; set; } = new Subscription[] { };

		[JsonProperty("texts", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[]? Texts { get; set; }
	}
}
