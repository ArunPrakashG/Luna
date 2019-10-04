using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Assistant.Schedulers {
	public class SchedulerConfig {
		public DateTime ScheduleTime { get; set; }
		public TimeSpan Interval { get; set; }
		public Action? ScheduledAction { get; set; }
		public string Guid { get; set; } = string.Empty;
		public Timer? SchedulerTimer { get; set; }
	}

	public class ScheduledTaskEventArgs {
		public DateTime ScheduledTime { get; set; }
		public DateTime InitialTime { get; set; }
		public Timer? SchedulerTimer { get; set; }
		public string Guid { get; set; } = string.Empty;
	}

	public class SchedulerService {
		public readonly List<SchedulerConfig> Configs = new List<SchedulerConfig>();
		private readonly Logger Logger = new Logger("SCHEDULER");
		public delegate void OnScheduledTimeReached(object sender, ScheduledTaskEventArgs e);
		public event OnScheduledTimeReached? ScheduledTimeReached;

		public SchedulerService() { }

		public void ScheduleTask(int hour, int min, double intervalInHour, Action task) {
			if (hour < 0 || min < 0 || intervalInHour < 0 || task == null) {
				return;
			}

			DateTime now = DateTime.Now;
			DateTime firstRun = new DateTime(now.Year, now.Month, now.Day, hour, min, 0, 0);

			if (now > firstRun) {
				firstRun = firstRun.AddDays(1);
			}

			TimeSpan timeToGo = firstRun - now;

			if (timeToGo <= TimeSpan.Zero) {
				timeToGo = TimeSpan.Zero;
			}

			Timer timer = new Timer(x => {
				task.Invoke();
			}, null, timeToGo, TimeSpan.FromHours(intervalInHour));

			SchedulerConfig config = new SchedulerConfig() {
				Guid = Guid.NewGuid().ToString(),
				Interval = TimeSpan.FromHours(intervalInHour),
				ScheduledAction = task,
				SchedulerTimer = timer,
				ScheduleTime = firstRun
			};

			Logger.Log($"Scheduled a task to run at {intervalInHour} hours from now.", AssistantCore.Enums.LogLevels.Trace);
			Configs.Add(config);
		}

		private Timer? ScheduleTask(DateTime dateTime, double intervalInHour, Action task) {
			if (intervalInHour < 0 || task == null) {
				return null;
			}

			DateTime now = DateTime.Now;

			if (now > dateTime) {
				dateTime = dateTime.AddDays(1);
			}
			TimeSpan timeToGo = dateTime - now;

			if (timeToGo <= TimeSpan.Zero) {
				timeToGo = TimeSpan.Zero;
			}

			Timer? scheduleTimer = null;
			scheduleTimer = new Timer(x => {
				task.Invoke();
				if (intervalInHour == 0) {
					scheduleTimer?.Dispose();
				}
			}, null, timeToGo, intervalInHour > 0 ? TimeSpan.FromHours(intervalInHour) : timeToGo);

			SchedulerConfig config = new SchedulerConfig() {
				Guid = Guid.NewGuid().ToString(),
				Interval = TimeSpan.FromHours(intervalInHour),
				ScheduledAction = task,
				SchedulerTimer = scheduleTimer,
				ScheduleTime = dateTime
			};

			Configs.Add(config);
			return scheduleTimer;
		}

		public void ScheduleTask(DateTime dateTime, TimeSpan intervalSpan, Action actionToRun) {
			DateTime now = DateTime.Now;

			if (now > dateTime) {
				dateTime = dateTime.AddDays(1);
			}

			TimeSpan fireTime = dateTime - now;

			if (fireTime <= TimeSpan.Zero) {
				fireTime = TimeSpan.Zero;
			}

			Timer scheduleTimer = new Timer(x => {
				actionToRun.Invoke();
			}, null, fireTime, intervalSpan);

			SchedulerConfig config = new SchedulerConfig() {
				Guid = Guid.NewGuid().ToString(),
				Interval = intervalSpan,
				ScheduledAction = actionToRun,
				SchedulerTimer = scheduleTimer,
				ScheduleTime = dateTime
			};

			Logger.Log($"Scheduled a task to run at {intervalSpan.Hours} hours from now.", AssistantCore.Enums.LogLevels.Trace);
			Configs.Add(config);
		}

		public void IntervalInSeconds(int hour, int sec, double interval, Action task) {
			interval /= 3600;
			ScheduleTask(hour, sec, interval, task);
		}

		public void RunAt(DateTime time, TimeSpan interval, Action task) => ScheduleTask(time, interval, task);

		public void ScheduleForTime(DateTime targetTime, double intervalinHours, string guid) {
			DateTime initialTime = DateTime.Now;
			Timer? timer = null;
			timer = ScheduleTask(targetTime, intervalinHours, action);

			void action() {
				if (ScheduledTimeReached != null) {
					ScheduledTimeReached.Invoke(this, new ScheduledTaskEventArgs() {
						Guid = guid,
						InitialTime = initialTime,
						ScheduledTime = targetTime,
						SchedulerTimer = timer
					});
				}
			}
		}
	}
}
