using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.Server.Authentication {
	public class UserAuthentication {

		private readonly Logger Logger = new Logger("KESTREL-AUTH");
		private static AuthenticationClientData PreviousAuthenticationData { get; set; }

		public bool IsClientAuthenticated(AuthenticationClientData clientToAuthenticate) {
			if (clientToAuthenticate == null) {
				Logger.Log("client data is null", Enums.LogLevels.Warn);
				return false;
			}

			if (KestrelServer.AuthenticatedClients.Contains(clientToAuthenticate)) {
				return true;
			}

			if (!Core.CoreInitiationCompleted) {
				Logger.Log("Core init process isn't completed.", Enums.LogLevels.Warn);
				return false;
			}

			Logger.Log($"Starting authentication for {clientToAuthenticate.ClientUserName}/{clientToAuthenticate.ClientEmailAddress}");

			if (clientToAuthenticate.ClientEmailAddress.Equals(PreviousAuthenticationData.ClientEmailAddress) &&
				(clientToAuthenticate.AuthRequestTime - PreviousAuthenticationData.AuthRequestTime).TotalSeconds < 5) {
				Logger.Log($"{clientToAuthenticate.ClientUserName} failed multiple times in last 5 seconds.", Enums.LogLevels.Warn);
				return false;
			}

			PreviousAuthenticationData = clientToAuthenticate;

			if (!Helpers.IsNullOrEmpty(clientToAuthenticate.ClientAuthToken) && !Helpers.IsNullOrEmpty(clientToAuthenticate.ClientDevice)) {
				if (Core.Config.VerifiedAuthenticationTokens != null && Core.Config.VerifiedAuthenticationTokens.Count > 0) {
					foreach (string token in Core.Config.VerifiedAuthenticationTokens) {
						if (token.Equals(clientToAuthenticate.ClientAuthToken)) {
							Logger.Log($"{clientToAuthenticate.ClientUserName} authenticated with assistant API.");
							KestrelServer.AuthenticatedClients.Add(clientToAuthenticate);
							return true;
						}
					}
				}
				else {
					Logger.Log("No verified authentication tokens present in config.", Enums.LogLevels.Trace);
					return false;
				}
			}

			return false;
		}

		public bool IsAllowedToExecute (AuthPostData postData) {
			if (postData == null) {
				return false;
			}

			if (Helpers.IsNullOrEmpty(postData.AuthToken)) {
				return false;
			}

			if (Helpers.IsNullOrEmpty(postData.DeviceName)) {
				return false;
			}

			if (Helpers.IsNullOrEmpty(postData.UserName)) {
				return false;
			}

			foreach (var client in KestrelServer.AuthenticatedClients) {
				if (client.ClientAuthToken.Equals(postData.AuthToken) &&
				    client.ClientDevice.Equals(postData.DeviceName) &&
				    client.ClientUserName.Equals(postData.UserName)) {
					return true;
				}
			}

			return false;
		}
	}
}
