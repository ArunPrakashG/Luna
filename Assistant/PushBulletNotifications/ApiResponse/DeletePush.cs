using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications.ApiResponse {
	public class DeletePush {

		[JsonProperty("error")]
		public Error ErrorReason { get; set; }
		[JsonProperty("error_code")]
		public string ErrorCode { get; set; }

		public class Error {
			[JsonProperty("code")]
			public string Code { get; set; }
			[JsonProperty("type")]
			public string Type { get; set; }
			[JsonProperty("message")]
			public string Message { get; set; }
			[JsonProperty("cat")]
			public string Cat { get; set; }
		}

	}
}
