using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class BaseRequest {
		[JsonProperty]
		public string RequestType { get; set; } = string.Empty;

		[JsonProperty]
		public string RequestObject { get; set; } = string.Empty;
	}
}
