using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Luna.Features {
	internal class JobScheduler {
		private readonly InternalLogger Logger = new InternalLogger(nameof(JobScheduler));
		private readonly JobInitializer JobInitializer = new JobInitializer();
		private readonly Dictionary<IScheduledInternalJob, Timer> Jobs = new Dictionary<IScheduledInternalJob, Timer>();

		internal void InitJobManager() {
			if (!JobInitializer.LoadInternalJobs()) {
				return;
			}

			for (int i = 0; i < JobInitializer.Count; i++) {
				IScheduledInternalJob job = JobInitializer[i];

				if (job.HasJobExpired) {
					continue;
				}

				Timer jobTimer = new Timer(
					state => job.Events.EventAction(job.Events.EventStateArguments),
					null,
					job.SpanUntilInitialCall,
					job.IsRecurring ? job.DelayBetweenCalls : Timeout.InfiniteTimeSpan
				);

				if (!Add(job, jobTimer)) {
					Logger.Trace($"Failed to initialize job '{job.JobName}'");
					continue;
				}

				Logger.Info($"'{job.JobName}' set to fire @ {Math.Round(job.SpanUntilInitialCall.TotalMinutes, 3)} minutes from now {(job.IsRecurring ? $"and {Math.Round(job.DelayBetweenCalls.TotalMinutes, 3)} minutes thereafter." : "")}");
			}
		}

		private bool Add(IScheduledInternalJob job, Timer jobTimer) {
			lock (Jobs) {
				return Jobs.TryAdd(job, jobTimer);
			}
		}
	}
}
