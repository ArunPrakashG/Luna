using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Responses {
	public class SuccessCommand {
		[JsonProperty]
		public string ResponseMessage { get; set; }
		[JsonProperty]
		public int ResponseCode { get; set; }
		[JsonProperty]
		public string JsonResponseObject { get; set; }
	}
}
