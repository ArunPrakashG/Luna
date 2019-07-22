using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.PushBulletNotifications.ApiResponse;
using Assistant.PushBulletNotifications.Exceptions;
using static Assistant.AssistantCore.Enums;

namespace Assistant.PushBulletNotifications {
	public class PushBulletService {
		private Logger Logger { get; set; } = new Logger("PUSH-BULLET-SERVICE");
		public PushBulletClient BulletClient { get; private set; }
		public UserDeviceList CachedPushDevices { get; private set; }
		private string AccessToken { get; set; }

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

		//TODO: init service
		public (bool status, UserDeviceList currentPushDevices) InitPushService() {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("No internet connection available. cannot connect to PushBullet API.", LogLevels.Error);
				return (false, CachedPushDevices);
			}

			BulletClient = new PushBulletClient(AccessToken);
			CachedPushDevices = BulletClient.GetCurrentDevices();

			return (false, null);
		}
	}
}
