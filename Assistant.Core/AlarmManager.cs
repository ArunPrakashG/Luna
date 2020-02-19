using Assistant.Logging;
using Assistant.Logging.Interfaces;
using FluentScheduler;
using System;
using System.Collections.Generic;

namespace Assistant.Core {
	public static class AlarmManager {
		public static readonly Dictionary<IAlarm, Schedule> Alarms = new Dictionary<IAlarm, Schedule>();
		private static readonly ILogger Logger = new Logger(typeof(AlarmManager).Name);

		public static AlarmResponse SetAlarm(IAlarm alarm) {
			if (alarm == null || string.IsNullOrEmpty(alarm.Name) || string.IsNullOrEmpty(alarm.Description) || alarm.Task == null) {
				return new AlarmResponse(false, null, DateTime.MinValue);
			}

			TimeSpan span = TimeSpan.FromSeconds(10);

			try {
				JobManager.AddJob(() => alarm.Task.Invoke(alarm), (s) => s.WithName(alarm.Id).ToRunOnceAt(alarm.At));
				Schedule sch = JobManager.GetSchedule(alarm.Id).NonReentrant();
				Alarms.Add(alarm, sch);
				Logger.Info($"Alarm set with name {alarm.Name} @ {alarm.At.ToString()}");
				return new AlarmResponse(!sch.Disabled, alarm.Id, sch.NextRun);
			}
			catch (Exception e) {
				Logger.Exception(e);
				return new AlarmResponse();
			}
		}
	}
}
