using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.PushBullet.ApiResponse;
using Assistant.PushBullet.Exceptions;
using Assistant.PushBullet.Parameters;
using System;
using static Assistant.AssistantCore.Enums;

namespace Assistant.PushBullet {
	public class PushBulletService {
		private Logger Logger { get; set; } = new Logger("PUSH-BULLET-SERVICE");
		public PushBulletClient BulletClient { get; private set; } = new PushBulletClient();
		public UserDeviceListResponse? CachedPushDevices { get; private set; } = new UserDeviceListResponse();
		public string AccessToken { get; private set; } = string.Empty;
		public bool IsBroadcastServiceOnline { get; private set; }
		private PushRequestContent PreviousBroadcastMessage { get; set; } = new PushRequestContent();

		public PushBulletService InitPushBulletService(string? accessToken) {
			if (accessToken == null || accessToken.IsNull()) {
				AccessToken = Core.Config.PushBulletApiKey ?? throw new IncorrectAccessTokenException();
				return this;
			}

			AccessToken = accessToken ?? throw new IncorrectAccessTokenException();
			return this;
		}

		public bool InitPushService() {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("No internet connection available. cannot connect to PushBullet API.", LogLevels.Error);
				return false;
			}

			BulletClient.InitPushBulletClient(AccessToken);

			try {
				CachedPushDevices = BulletClient.GetCurrentDevices();
			}
			catch (RequestFailedException) {
				Logger.Log("Request to the api failed", LogLevels.Warn);
				return false;
			}
			catch (IncorrectAccessTokenException) {
				Logger.Log("The specified access token is invalid", LogLevels.Warn);
				return false;
			}
			catch (NullReferenceException) {
				return false;
			}

			if (CachedPushDevices == null) {
				Logger.Log("Failed to load PushBullet serivce.", LogLevels.Warn);
				return false;
			}

			Logger.Log("Loaded PushBullet service.");
			IsBroadcastServiceOnline = true;
			return true;
		}

		public (bool broadcastStatus, PushResponse? response) BroadcastMessage(PushRequestContent broadcastValue) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("No internet connection available. cannot connect to PushBullet API.", LogLevels.Error);
				return (false, null);
			}

			if (!IsBroadcastServiceOnline) {
				return (false, null);
			}

			if (PreviousBroadcastMessage.Equals(broadcastValue)) {
				return (false, null);
			}

			if (broadcastValue == null) {
				Logger.Log("Cannot broadcast as the required values are empty.", LogLevels.Warn);
				return (false, null);
			}

			PreviousBroadcastMessage = broadcastValue;
			PushResponse? pushResponse;
			try {
				pushResponse = BulletClient.SendPush(broadcastValue);
			}
			catch (ParameterValueIsNullException) {
				Logger.Log("Parameter value is null.", LogLevels.Warn);
				return (false, null);
			}
			catch (ResponseIsNullException) {
				Logger.Log("The api response is null", LogLevels.Warn);
				return (false, null);
			}
			catch (RequestFailedException) {
				Logger.Log("Request to the api failed", LogLevels.Warn);
				return (false, null);
			}
			catch (NullReferenceException) {
				return (false, null);
			}

			return (true, pushResponse);
		}
	}
}
