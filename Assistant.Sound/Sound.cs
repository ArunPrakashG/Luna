using System;

namespace Assistant.Sound
{
	public class Sound
	{
		public static void PlayNotification(Enums.NotificationContext context = Enums.NotificationContext.Normal, bool redirectOutput = false) {
			if (Core.IsUnknownOs) {
				Logger.Log("Cannot proceed as the running operating system is unknown.", Enums.LogLevels.Error);
				return;
			}

			if (Core.Config.MuteAssistant) {
				Logger.Log("Notifications are muted in config.", Enums.LogLevels.Trace);
				return;
			}

			if (!Directory.Exists(Constants.ResourcesDirectory)) {
				Logger.Log("Resources directory doesn't exist!", Enums.LogLevels.Warn);
				return;
			}

			switch (context) {
				case Enums.NotificationContext.Imap:
					if (!File.Exists(Constants.IMAPPushNotificationFilePath)) {
						Logger.Log("IMAP notification music file doesn't exist!", Enums.LogLevels.Warn);
						return;
					}

					ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.IMAPPushFileName} -q", Core.Config.Debug || redirectOutput);
					Logger.Log("Notification command processed sucessfully!", Enums.LogLevels.Trace);
					break;

				case Enums.NotificationContext.EmailSend:
					break;

				case Enums.NotificationContext.EmailSendFailed:
					break;

				case Enums.NotificationContext.FatalError:
					break;

				case Enums.NotificationContext.Normal:
					if (!Core.IsNetworkAvailable) {
						Logger.Log("Cannot process, network is unavailable.", Enums.LogLevels.Warn);
					}
					break;
			}
		}
	}
}
