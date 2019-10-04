using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class RemainderRequest {
		[JsonProperty]
		public string Message { get; set; } = string.Empty;

		[JsonProperty]
		public int MinutesUntilRemainding { get; set; }
	}
}
