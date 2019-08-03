using Newtonsoft.Json;

namespace Assistant.PushBullet.ApiResponse {
	public class UserDeviceListResponse {

		[JsonProperty("accounts")]
		public object[] Accounts { get; set; }
		[JsonProperty("blocks")]
		public object[] Blocks { get; set; }
		[JsonProperty("channels")]
		public object[] Channels { get; set; }
		[JsonProperty("chats")]
		public object[] Chats { get; set; }
		[JsonProperty("clients")]
		public object[] Clients { get; set; }
		[JsonProperty("contacts")]
		public object[] Contacts { get; set; }
		[JsonProperty("devices")]
		public Device[] Devices { get; set; }
		[JsonProperty("grants")]
		public object[] Grants { get; set; }
		[JsonProperty("pushes")]
		public object[] Pushes { get; set; }
		[JsonProperty("profiles")]
		public object[] Profiles { get; set; }
		[JsonProperty("subscriptions")]
		public object[] Subscriptions { get; set; }
		[JsonProperty("texts")]
		public object[] Texts { get; set; }

		public class Device {
			[JsonProperty("active")]
			public bool CurrentlyActive { get; set; }
			[JsonProperty("iden")]
			public string Identifier { get; set; }
			[JsonProperty("created")]
			public float Created { get; set; }
			[JsonProperty("modified")]
			public float LastModified { get; set; }
			[JsonProperty("type")]
			public string DeviceType { get; set; }
			[JsonProperty("kind")]
			public string DeviceKind { get; set; }
			[JsonProperty("nickname")]
			public string DeviceNickname { get; set; }
			[JsonProperty("manufacturer")]
			public string DeviceManufacturer { get; set; }
			[JsonProperty("model")]
			public string DeviceModel { get; set; }
			[JsonProperty("app_version")]
			public int AppVersion { get; set; }
			[JsonProperty("pushable")]
			public bool Pushable { get; set; }
			[JsonProperty("icon")]
			public string Icon { get; set; }
			[JsonProperty("fingerprint")]
			public string DeviceFingerprint { get; set; }
			[JsonProperty("push_token")]
			public string PushToken { get; set; }
			[JsonProperty("has_sms")]
			public bool HasSMSAbility { get; set; }
			[JsonProperty("has_mms")]
			public bool HasMMSAbility { get; set; }
			[JsonProperty("remote_files")]
			public string RemoteFiles { get; set; }
		}
	}
}
