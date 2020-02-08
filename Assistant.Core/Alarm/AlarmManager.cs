using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.Core.Alarm {
	public class AlarmManager {
		public static Dictionary<AlarmConfig, SchedulerConfig> Alarms { get; private set; } = new Dictionary<AlarmConfig, SchedulerConfig>();
		private readonly Logger Logger = new Logger("ALARM");

		public bool SetAlarm(int hoursFromNow, string alarmMessage, bool useTTS, TimeSpan repeatInterval, bool repeat = false) {
			if (hoursFromNow <= 0 || string.IsNullOrEmpty(alarmMessage)) {
				return false;
			}

			string guid = Guid.NewGuid().ToString();

			AlarmConfig alarm = new AlarmConfig() {
				AlarmAt = DateTime.Now.AddHours(hoursFromNow),
				AlarmGuid = guid,
				AlarmMessage = alarmMessage,
				ShouldRepeat = repeat,
				ShouldOverideSoundSetting = false,
				ShouldUseTTS = useTTS
			};

			SchedulerConfig config = new SchedulerConfig() {
				Guid = guid,
				ScheduledSpan = TimeSpan.FromHours(hoursFromNow),
				RepeatInterval = repeatInterval
			};

			config.SchedulerObjects.Add(alarm);

			if (alarm.Scheduler != null && alarm.Scheduler.SetScheduler(config)) {
				alarm.Scheduler.ScheduledTimeReached += OnScheduledTimeReached;
				Alarms.TryAdd(alarm, config);
				Logger.Log($"An alarm has been set at {hoursFromNow} hours from now.");
				return true;
			}

			Logger.Log("Failed to set alarm.", LogLevels.Warn);
			return false;
		}

		private void OnScheduledTimeReached(object sender, ScheduledTaskEventArgs e) {
			if (Alarms.Count <= 0) {
				return;
			}

			if (sender == null || e == null) {
				return;
			}

			AlarmConfig? configToRemove = new AlarmConfig();

			foreach (KeyValuePair<AlarmConfig, SchedulerConfig> alarmConfig in Alarms) {
				if (alarmConfig.Key == null || alarmConfig.Value == null) {
					continue;
				}

				if (alarmConfig.Value.Guid != null && alarmConfig.Value.Guid.Equals(e.Guid)) {
					Logger.Log($"ALARM >>> {alarmConfig.Key.AlarmMessage}");

					if (alarmConfig.Key.ShouldUseTTS) {
						Task.Run(async () => await TTS.SpeakText($"Sir, {alarmConfig.Key.AlarmMessage}", true).ConfigureAwait(false));
					}

					if (alarmConfig.Key.AlarmGuid != null) {
						Helpers.InBackgroundThread(async () => await PlayAlarmSound(alarmConfig.Key.AlarmGuid).ConfigureAwait(false));
					}

					if (alarmConfig.Key.ShouldRepeat && !alarmConfig.Key.Snooze) {
						alarmConfig.Key.Scheduler = null;
						alarmConfig.Key.Scheduler = new Scheduler();
						if (e.SchedulerConfig != null) {

							SchedulerConfig config = new SchedulerConfig() {
								Guid = e.Guid,
								ScheduledSpan = e.SchedulerConfig.RepeatInterval,
								RepeatInterval = e.SchedulerConfig.RepeatInterval
							};

							config.SchedulerObjects.Add(alarmConfig.Key);
							alarmConfig.Key.Scheduler.SetScheduler(config);
							alarmConfig.Key.Scheduler.ScheduledTimeReached += OnScheduledTimeReached;
							Logger.Log($"Alarm will repeat exactly after {e.SchedulerConfig.RepeatInterval.Hours} from now.");
						}
					}
					else {
						configToRemove = alarmConfig.Key;
					}
				}
			}

			if (configToRemove != null) {
				Alarms.Remove(configToRemove);
			}
		}

		private async Task PlayAlarmSound(string guid) {
			if (!File.Exists(Constants.AlarmFilePath)) {
				return;
			}

			if (Core.PiController == null || !Core.PiController.IsControllerProperlyInitialized) {
				return;
			}

			foreach (KeyValuePair<AlarmConfig, SchedulerConfig> alarm in Alarms) {
				if (alarm.Key.AlarmGuid == guid) {

					if (alarm.Key.ShouldOverideSoundSetting) {
						await Core.PiController.GetSoundController().SetPiVolume(90).ConfigureAwait(false);
					}

					while (!alarm.Key.Snooze) {
						string executeResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.AlarmFileName} -q".ExecuteBash(false);
						await Task.Delay(3000).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
