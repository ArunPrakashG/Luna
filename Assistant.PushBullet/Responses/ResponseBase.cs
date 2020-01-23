using Assistant.PushBullet.Responses.Chats;
using Assistant.PushBullet.Responses.Devices;
using Assistant.PushBullet.Responses.Pushes;
using Assistant.PushBullet.Responses.Subscriptions;
using Newtonsoft.Json;
using System;

namespace Assistant.PushBullet.Responses {
	[Serializable]
	public class ResponseBase {
		[JsonIgnore]
		public bool IsDeleteRequestSuccess { get; set; }

		[JsonProperty("accounts", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Accounts { get; set; }

		[JsonProperty("blocks", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Blocks { get; set; }

		[JsonProperty("channels", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Channels { get; set; }

		[JsonProperty("chats", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public ChatsBase[] Chats { get; set; }

		[JsonProperty("clients", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Clients { get; set; }

		[JsonProperty("contacts", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Contacts { get; set; }

		[JsonProperty("devices", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public DevicesBase[] Devices { get; set; }

		[JsonProperty("grants", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Grants { get; set; }

		[JsonProperty("pushes", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public PushesBase[] Pushes { get; set; }

		[JsonProperty("profiles", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Profiles { get; set; }

		[JsonProperty("subscriptions", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public SubscriptionsBase[] Subscriptions { get; set; }

		[JsonProperty("texts", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
		public object[] Texts { get; set; }
	}
}
