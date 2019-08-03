using System;
using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.PushBullet.ApiResponse;
using Assistant.PushBullet.Exceptions;
using Assistant.PushBullet.Parameters;
using static Assistant.AssistantCore.Enums;

namespace Assistant.PushBullet {
	public class PushBulletService {
		private Logger Logger { get; set; } = new Logger("PUSH-BULLET-SERVICE");
		public PushBulletClient BulletClient { get; private set; }
		public UserDeviceListResponse CachedPushDevices { get; private set; }
		private string AccessToken { get; set; }
		public bool IsBroadcastServiceOnline { get; set; }
		private PushRequestContent PreviousBroadcastMessage { get; set; } = new PushRequestContent();

		public PushBulletService(string accessToken) {
			if (!Helpers.IsNullOrEmpty(accessToken)) {
				AccessToken = accessToken;
			}
			else {
				throw new IncorrectAccessTokenException(Core.Config.PushBulletApiKey);
			}
		}

		public PushBulletService() {
			if (!Helpers.IsNullOrEmpty(Core.Config.PushBulletApiKey)) {
				AccessToken = Core.Config.PushBulletApiKey;
			}
			else {
				throw new IncorrectAccessTokenException(Core.Config.PushBulletApiKey);
			}
		}

		public (bool status, UserDeviceListResponse currentPushDevices) InitPushService() {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("No internet connection available. cannot connect to PushBullet API.", LogLevels.Error);
				return (false, CachedPushDevices);
			}

			BulletClient = new PushBulletClient(AccessToken);

			try {
				CachedPushDevices = (UserDeviceListResponse) BulletClient.GetCurrentDevices();
			}
			catch (RequestFailedException) {
				Logger.Log("Request to the api failed", LogLevels.Warn);
				return (false, CachedPushDevices);
			}
			catch (IncorrectAccessTokenException) {
				Logger.Log("The specified access token is invalid", LogLevels.Warn);
				return (false, CachedPushDevices);
			}
			catch (NullReferenceException) {
				return (false, CachedPushDevices);
			}

			if (CachedPushDevices == null) {
				Logger.Log("Failed to load PushBullet serivce.", LogLevels.Warn);
				return (false, CachedPushDevices);
			}

			Logger.Log("Loaded PushBullet service.");
			IsBroadcastServiceOnline = true;
			return (true, CachedPushDevices);
		}

		public (bool broadcastStatus, PushResponse response) BroadcastMessage(PushRequestContent broadcastValue) {
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
			PushResponse pushResponse;
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
