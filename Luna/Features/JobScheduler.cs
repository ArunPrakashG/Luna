using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Luna.Features {
	internal class JobScheduler {
		private readonly InternalLogger Logger = new InternalLogger(nameof(JobScheduler));
		private readonly JobInitializer JobInitializer = new JobInitializer();
		private readonly Dictionary<InternalJobBase, Timer> Jobs = new Dictionary<InternalJobBase, Timer>();

		internal void InitJobManager() {
			if (!JobInitializer.LoadInternalJobs()) {
				return;
			}

			InitializeJobs();
		}

		internal void InitializeJobs() {
			for (int i = 0; i < JobInitializer.JobCount; i++) {
				InternalJobBase job = JobInitializer[i];
				Helpers.InBackgroundThread(job.OnJobLoaded);

				if (job.HasJobExpired) {
					job.Dispose();
					Jobs.Remove(job);
					continue;
				}

				Timer jobTimer = new Timer(
					state => job.OnJobOccured(job.JobParameters),
					null,
					job.SpanUntilInitialCall,
					job.IsRecurring ? job.DelayBetweenCalls : Timeout.InfiniteTimeSpan
				);
				
				if (!TryAdd(job, jobTimer)) {
					Logger.Trace($"Failed to initialize job '{job.JobName}'");
					continue;
				}

				Logger.Info($"'{job.JobName}' set to fire @ {Math.Round(job.SpanUntilInitialCall.TotalMinutes, 3)} minutes from now {(job.IsRecurring ? $"and {Math.Round(job.DelayBetweenCalls.TotalMinutes, 3)} minutes thereafter." : "")}");
			}
		}

		private bool TryAdd(InternalJobBase job, Timer jobTimer) {
			if(job == null || jobTimer == null) {
				return false;
			}

			Helpers.InBackgroundThread(job.OnJobInitialized);
			job.SetJobTimer(jobTimer);

			lock (Jobs) {
				return Jobs.TryAdd(job, jobTimer);
			}
		}
	}
}
