using Newtonsoft.Json;

namespace Assistant.Server.TCPServer.Responses {
	public class ResponseBase {
		[JsonProperty]
		public CommandEnums.CommandResponseCode ResponseCode { get; set; }

		[JsonProperty]
		public CommandEnums.ResponseObjectType ResponseType { get; set; }

		[JsonProperty]
		public string? ResponseMessage { get; set; } = string.Empty;

		[JsonProperty]
		public string? ResponseJson { get; set; } = string.Empty;

		public ResponseBase(CommandEnums.CommandResponseCode responseCode, CommandEnums.ResponseObjectType respType, string? respMsg, string? respJson) {
			ResponseCode = responseCode;
			ResponseType = respType;
			ResponseMessage = respMsg;
			ResponseJson = respJson;
		}

		public string AsJson() => JsonConvert.SerializeObject(this);
	}
}
