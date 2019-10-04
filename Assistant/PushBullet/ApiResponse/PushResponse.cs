using Newtonsoft.Json;

namespace Assistant.PushBullet.ApiResponse {
	public class PushResponse {
		[JsonProperty("active")]
		public bool IsActive { get; set; }
		[JsonProperty("iden")]
		public string PushIdentifier { get; set; } = string.Empty;
		[JsonProperty("created")]
		public float CreatedAt { get; set; }
		[JsonProperty("modified")]
		public float ModifiedAt { get; set; }
		[JsonProperty("type")]
		public string PushType { get; set; } = string.Empty;
		[JsonProperty("dismissed")]
		public bool IsDismissed { get; set; }
		[JsonProperty("direction")]
		public string Direction { get; set; } = string.Empty;
		[JsonProperty("sender_iden")]
		public string SenderIdentifier { get; set; } = string.Empty;
		[JsonProperty("sender_email")]
		public string SenderEmail { get; set; } = string.Empty;
		[JsonProperty("sender_email_normalized")]
		public string SenderEmailNormalized { get; set; } = string.Empty;
		[JsonProperty("sender_name")]
		public string SenderName { get; set; } = string.Empty;
		[JsonProperty("receiver_iden")]
		public string ReceiverIdentifier { get; set; } = string.Empty;
		[JsonProperty("receiver_email")]
		public string ReceiverEmail { get; set; } = string.Empty;
		[JsonProperty("receiver_email_normalized")]
		public string ReceiverEmailNormalized { get; set; } = string.Empty;
		[JsonProperty("title")]
		public string PushTitle { get; set; } = string.Empty;
		[JsonProperty("body")]
		public string PushBody { get; set; } = string.Empty;
	}
}
