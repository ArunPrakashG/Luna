using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class GpioRequest {
		[JsonProperty]
		public string Command { get; set; }

		[JsonProperty]
		public List<string> StringParameters { get; set; }
	}
}
