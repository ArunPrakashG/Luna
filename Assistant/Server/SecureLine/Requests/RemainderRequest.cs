using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class RemainderRequest {
		[JsonProperty]
		public string Message { get; set; }

		[JsonProperty]
		public int MinutesUntilRemainding { get; set; }
	}
}
