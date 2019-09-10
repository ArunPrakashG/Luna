using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class BaseRequest {
		[JsonProperty]
		public string RequestType { get; set; }

		[JsonProperty]
		public string RequestObject { get; set; }
	}
}
