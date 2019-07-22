using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications.ApiResponse {
	public class ListPushes {
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
		public object[] Cevices { get; set; }
		[JsonProperty("grants")]
		public object[] Grants { get; set; }
		[JsonProperty("pushes")]
		public Push[] Pushes { get; set; }
		[JsonProperty("profiles")]
		public object[] Profiles { get; set; }
		[JsonProperty("subscriptions")]
		public object[] Subscriptions { get; set; }
		[JsonProperty("texts")]
		public object[] Texts { get; set; }

		public class Push {
			[JsonProperty("accounts")]
			public bool IsActive { get; set; }
			[JsonProperty("iden")]
			public string Identifier { get; set; }
			[JsonProperty("created")]
			public float CreatedAt { get; set; }
			[JsonProperty("modified")]
			public float ModifedAt { get; set; }
			[JsonProperty("type")]
			public string Type { get; set; }
			[JsonProperty("dismissed")]
			public bool IsDismissed { get; set; }
			[JsonProperty("guid")]
			public string Guid { get; set; }
			[JsonProperty("direction")]
			public string Direction { get; set; }
			[JsonProperty("sender_iden")]
			public string SenderIdentifier { get; set; }
			[JsonProperty("sender_email")]
			public string SenderEmail { get; set; }
			[JsonProperty("sender_email_normalized")]
			public string SenderEmailNormalized { get; set; }
			[JsonProperty("sender_name")]
			public string SenderName { get; set; }
			[JsonProperty("receiver_iden")]
			public string ReceiverIdentifier { get; set; }
			[JsonProperty("receiver_email")]
			public string ReceiverEmail { get; set; }
			[JsonProperty("receiver_email_normalized")]
			public string ReceiverEmailNormalized { get; set; }
			[JsonProperty("source_device_iden")]
			public string SourceDeviceIdentifier { get; set; }
			[JsonProperty("awake_app_guids")]
			public string[] AwakeAppGuids { get; set; }
			[JsonProperty("body")]
			public string PushBody { get; set; }
			[JsonProperty("title")]
			public string PushTitle { get; set; }
		}

	}
}
