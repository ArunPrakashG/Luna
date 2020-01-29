using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using System.Collections.Generic;

namespace Assistant.Servers.Kestrel.Authentication {
	public class UserAuthentication {

		private readonly Logger Logger = new Logger("KESTREL-AUTH");

		public bool IsAllowedToExecute(string apiKey) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return false;
			}

			foreach (KeyValuePair<string, int> client in Core.Config.AuthenticatedTokens) {
				if (!Helpers.IsNullOrEmpty(client.Key)) {
					if (client.Key.Equals(apiKey)) {
						return true;
					}
				}
			}

			return false;
		}
	}
}
