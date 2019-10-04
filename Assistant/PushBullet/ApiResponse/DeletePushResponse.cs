using Newtonsoft.Json;

namespace Assistant.PushBullet.ApiResponse {
	public class DeletePushResponse {

		[JsonProperty("error")]
		public Error? ErrorReason { get; set; }
		[JsonProperty("error_code")]
		public string ErrorCode { get; set; } = string.Empty;

		public class Error {
			[JsonProperty("code")]
			public string Code { get; set; } = string.Empty;
			[JsonProperty("type")]
			public string Type { get; set; } = string.Empty;
			[JsonProperty("message")]
			public string Message { get; set; } = string.Empty;
			[JsonProperty("cat")]
			public string Cat { get; set; } = string.Empty;
		}

	}
}
