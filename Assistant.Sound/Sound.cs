using Assistant.Extensions;
using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.IO;
using System.Runtime.InteropServices;
using static Assistant.Logging.Enums;

namespace Assistant.Sound {
	public class Sound : IExternal {
		private static readonly ILogger Logger = new Logger(typeof(Sound).Name);
		public static bool IsGloballyMuted = false;
		public static bool IsSoundAllowed => !IsGloballyMuted && Helpers.GetOsPlatform() == OSPlatform.Linux;

		public Sound(bool isMuted) {
			IsGloballyMuted = isMuted;
		}

		public static void PlayNotification(ENOTIFICATION_CONTEXT context = ENOTIFICATION_CONTEXT.NORMAL, bool redirectOutput = false) {
			if (Helpers.GetOsPlatform() != OSPlatform.Linux) {
				Logger.Log("Cannot proceed as the running operating system is unknown.", LogLevels.Error);
				return;
			}

			if (IsGloballyMuted) {
				Logger.Trace("Notifications are muted globally.");
				return;
			}

			if (!Directory.Exists(Constants.ResourcesDirectory)) {
				Logger.Warning("Resources directory doesn't exist!");
				return;
			}

			switch (context) {
				case ENOTIFICATION_CONTEXT.NORMAL:
					break;
				case ENOTIFICATION_CONTEXT.ALERT:					
					if (!File.Exists(Constants.ALERT_SOUND_PATH)) {
						Logger.Warning("Alert sound file doesn't exist!");
						return;
					}

					if(redirectOutput)
						Logger.Info($"cd /home/pi/Desktop/HomeAssistant/Assistant.Core/ && play {Constants.ALERT_SOUND_PATH} -q".ExecuteBash(false));
					else
						Logger.Trace($"cd /home/pi/Desktop/HomeAssistant/Assistant.Core/ && play {Constants.ALERT_SOUND_PATH} -q".ExecuteBash(false));
					break;
				case ENOTIFICATION_CONTEXT.ERROR:
					break;
			}
		}

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);

		public enum ENOTIFICATION_CONTEXT : byte {
			NORMAL,
			ERROR,
			ALERT
		}
	}
}
