using Newtonsoft.Json;

namespace Assistant.Servers.SecureLine.Requests {
	public class RemainderRequest {
		[JsonProperty]
		public string Message { get; set; } = string.Empty;

		[JsonProperty]
		public int MinutesUntilRemainding { get; set; }
	}
}
