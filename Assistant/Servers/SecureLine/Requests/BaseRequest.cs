using Newtonsoft.Json;

namespace Assistant.Servers.SecureLine.Requests {
	public class BaseRequest {
		[JsonProperty]
		public string RequestType { get; set; } = string.Empty;

		[JsonProperty]
		public string RequestObject { get; set; } = string.Empty;
	}
}
