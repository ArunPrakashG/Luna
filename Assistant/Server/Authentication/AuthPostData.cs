using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server.Authentication {
	public class AuthPostData {
		public string AuthToken { get; set; }
		public string ClientEmailId { get; set; }
	}
}
