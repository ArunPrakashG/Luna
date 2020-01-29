using Newtonsoft.Json;
using System.Collections.Generic;

namespace Assistant.Servers.SecureLine.Requests {
	public class GpioRequest {
		[JsonProperty]
		public string Command { get; set; } = string.Empty;

		[JsonProperty]
		public List<string> StringParameters { get; set; } = new List<string>();
	}
}
