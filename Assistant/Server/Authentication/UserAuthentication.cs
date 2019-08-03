using System;
using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.Server.Authentication {
	public class UserAuthentication {

		private readonly Logger Logger = new Logger("KESTREL-AUTH");
		private static AuthenticationClientData PreviousAuthenticationData { get; set; }

		public bool AuthenticateClient(AuthenticationClientData clientToAuthenticate) {
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

			if (Helpers.IsNullOrEmpty(clientToAuthenticate.ClientAuthToken) ||
				Helpers.IsNullOrEmpty(clientToAuthenticate.ClientEmailAddress)) {
				return false;
			}

			Logger.Log($"Starting authentication for {clientToAuthenticate.ClientEmailAddress}/{clientToAuthenticate.ClientAuthToken}");
			
			if (PreviousAuthenticationData != null && clientToAuthenticate.ClientEmailAddress.Equals(PreviousAuthenticationData.ClientEmailAddress) &&
				(clientToAuthenticate.AuthRequestTime - PreviousAuthenticationData.AuthRequestTime).TotalSeconds < 5) {
				Logger.Log($"{clientToAuthenticate.ClientEmailAddress} failed multiple times in last 5 seconds.", Enums.LogLevels.Warn);
				return false;
			}

			PreviousAuthenticationData = clientToAuthenticate;

			if (!Helpers.IsNullOrEmpty(clientToAuthenticate.ClientAuthToken) && !Helpers.IsNullOrEmpty(clientToAuthenticate.ClientEmailAddress)) {
				if (Core.Config.ClientAuthenticationData != null && Core.Config.ClientAuthenticationData.Count > 0) {
					foreach (AuthenticationClientData client in Core.Config.ClientAuthenticationData) {
						if (client != null && client.ClientAuthToken.Equals(clientToAuthenticate.ClientAuthToken) && client.ClientEmailAddress.Equals(clientToAuthenticate.ClientEmailAddress)) {
							Logger.Log($"{clientToAuthenticate.ClientEmailAddress} authenticated with assistant API.");
							client.IsAuthenticated = true;
							clientToAuthenticate.IsAuthenticated = true;
							client.AuthenticatedUntil = client.AuthRequestTime.AddHours(1);
							clientToAuthenticate.AuthenticatedUntil = clientToAuthenticate.AuthRequestTime.AddHours(1);
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

		public bool IsAllowedToExecute(AuthPostData postData) {
			if (postData == null) {
				return false;
			}

			if (Helpers.IsNullOrEmpty(postData.AuthToken)) {
				return false;
			}

			if (Helpers.IsNullOrEmpty(postData.ClientEmailId)) {
				return false;
			}

			foreach (AuthenticationClientData client in KestrelServer.AuthenticatedClients) {
				if (client.ClientAuthToken.Equals(postData.AuthToken) && client.ClientEmailAddress.Equals(postData.ClientEmailId)) {
					if (client.AuthenticatedUntil > DateTime.Now) {
						return client.IsAuthenticated;
					}
					else {
						client.IsAuthenticated = false;
						KestrelServer.AuthenticatedClients.Remove(client);
						Logger.Log($"Removed a client from authenticated list as the time period is over. ({client.ClientEmailAddress})", Enums.LogLevels.Trace);
						return false;
					}
				}
			}

			return false;
		}
	}
}
