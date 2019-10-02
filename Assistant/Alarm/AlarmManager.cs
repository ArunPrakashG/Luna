using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Schedulers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.Alarm {
	public class Alarm {
		[JsonProperty]
		public string AlarmMessage { get; set; }

		[JsonProperty]
		public DateTime AlarmAt { get; set; }

		[JsonProperty]
		public bool IsCompleted { get; set; }

		[JsonProperty]
		public string AlarmGuid { get; set; }

		[JsonProperty]
		public bool ShouldRepeat { get; set; }

		[JsonProperty]
		public bool ShouldUseTTS { get; set; }

		[JsonProperty]
		public bool ShouldOverideSoundSetting { get; set; }

		[JsonProperty]
		public bool Snooze { get; set; }
	}

	public class AlarmManager {
		public static List<Alarm> Alarms { get; set; } = new List<Alarm>();
		private readonly Logger Logger = new Logger("ALARM");

		public bool SetAlarm(int hoursFromNow, string alarmMessage, bool useTTS, bool repeat = false, int repeatHours = 0) {
			if (hoursFromNow <= 0 || Helpers.IsNullOrEmpty(alarmMessage)) {
				return false;
			}

			string guid = Guid.NewGuid().ToString();
			Core.Scheduler.ScheduledTimeReached += OnScheduledTimeReached;

			Alarm alarm = new Alarm() {
				AlarmAt = DateTime.Now.AddHours(hoursFromNow),
				AlarmGuid = guid,
				AlarmMessage = alarmMessage,
				IsCompleted = false,
				ShouldRepeat = repeat,
				ShouldOverideSoundSetting = false,
				ShouldUseTTS = useTTS
			};

			Core.Scheduler.ScheduleForTime(DateTime.Now.AddHours(hoursFromNow), repeatHours, guid);
			Alarms.Add(alarm);
			Logger.Log($"An alarm has been set at {hoursFromNow} hours from now.");
			return true;
		}

		private void OnScheduledTimeReached(object sender, ScheduledTaskEventArgs e) {
			if (Alarms.Count <= 0) {
				return;
			}

			foreach (Alarm alarm in Alarms) {
				if (alarm.AlarmGuid == e.Guid) {
					Logger.Log($"ALARM >>> {alarm.AlarmMessage}");
					if (alarm.ShouldUseTTS) {
						Task.Run(async () => await TTSService.SpeakText($"Sir, {alarm.AlarmMessage}", true).ConfigureAwait(false));
					}

					Helpers.InBackgroundThread(async () => await PlayAlarmSound(alarm.AlarmGuid).ConfigureAwait(false));
					alarm.IsCompleted = true;

					if (!alarm.ShouldRepeat) {
						foreach (SchedulerConfig task in Core.Scheduler.Configs) {
							if (task.Guid == e.Guid) {
								if (task.SchedulerTimer != null) {
									task.SchedulerTimer.Dispose();
								}
							}
						}
					}
					
					return;
				}
			}
		}

		private async Task PlayAlarmSound(string guid) {
			if (!File.Exists(Constants.AlarmFilePath)) {
				return;
			}

			await Core.PiController.PiSound.SetPiVolume(90).ConfigureAwait(false);
			foreach (Alarm alarm in Alarms) {
				if (alarm.AlarmGuid == guid) {
					while (!alarm.Snooze) {
						string executeResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.AlarmFileName} -q".ExecuteBash();
						await Task.Delay(1000).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
