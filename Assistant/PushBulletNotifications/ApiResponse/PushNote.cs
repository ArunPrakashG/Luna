using Newtonsoft.Json;

namespace Assistant.PushBulletNotifications.ApiResponse {
	public class PushNote {
		[JsonProperty("active")]
		public bool IsActive { get; set; }
		[JsonProperty("iden")]
		public string PushIdentifier { get; set; }
		[JsonProperty("created")]
		public float CreatedAt { get; set; }
		[JsonProperty("modified")]
		public float ModifiedAt { get; set; }
		[JsonProperty("type")]
		public string PushType { get; set; }
		[JsonProperty("dismissed")]
		public bool IsDismissed { get; set; }
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
		[JsonProperty("title")]
		public string PushTitle { get; set; }
		[JsonProperty("body")]
		public string PushBody { get; set; }
	}
}
