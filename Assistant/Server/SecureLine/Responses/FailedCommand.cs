using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Responses {
	public class FailedCommand {
		[JsonProperty]
		public int ResponseCode { get; set; }
		[JsonProperty]
		public string FailReason { get; set; }
	}
}
