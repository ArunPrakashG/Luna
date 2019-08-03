using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server.Authentication {
	public class AuthenticationClientData {
		public string ClientAuthToken { get; set; }
		public DateTime AuthRequestTime { get; set; }
		public string ClientUserName { get; set; }
		public string ClientDevice { get; set; }
		public string ClientEmailAddress { get; set; }
	}
}
