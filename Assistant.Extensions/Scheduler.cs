using System;
using System.Collections.Generic;

namespace Assistant.Extensions {
	public class Scheduler : IDisposable {
		public List<SchedulerConfig>? Schedulers = new List<SchedulerConfig>();
		public delegate void OnScheduledTimeReached(object sender, ScheduledTaskEventArgs e);
		public event OnScheduledTimeReached? ScheduledTimeReached;

		public bool SetScheduler(SchedulerConfig config) {
			if (config == null) {
				return false;
			}

			Helpers.ScheduleTask(() => {
				ScheduledTimeReached?.Invoke(this, new ScheduledTaskEventArgs() {
					Guid = config.Guid,
					SchedulerConfig = config
				});
			}, config.ScheduledSpan);

			return true;
		}

		public void Dispose() {
			Schedulers = null;
			ScheduledTimeReached = null;
		}
	}
}
