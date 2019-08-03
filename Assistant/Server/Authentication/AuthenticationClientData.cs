using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.Authentication {
	public class AuthenticationClientData {
		[JsonProperty]
		public string ClientAuthToken { get; set; }
		[JsonProperty]
		public DateTime AuthRequestTime { get; set; }
		[JsonProperty]
		public DateTime AuthenticatedUntil { get; set; }
		[JsonProperty]
		public bool IsAuthenticated { get; set; }
		[JsonProperty]
		public string ClientEmailAddress { get; set; }
	}
}
