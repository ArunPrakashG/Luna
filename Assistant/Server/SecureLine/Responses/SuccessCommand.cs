using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Responses {
	public class SuccessCommand {
		[JsonProperty]
		public string ResponseMessage { get; set; } = string.Empty;
		[JsonProperty]
		public int ResponseCode { get; set; }
		[JsonProperty]
		public string JsonResponseObject { get; set; } = string.Empty;
	}
}
