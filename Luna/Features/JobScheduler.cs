using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Threading;

namespace Luna.Features {
	internal static class JobScheduler {
		private static readonly InternalLogger Logger = new InternalLogger(nameof(JobScheduler));
		private static readonly JobInitializer JobInitializer;

		static JobScheduler() {
			JobInitializer = new JobInitializer();
		}

		internal static void InitJobManager() {
			//if (!JobInitializer.LoadInternalJobs()) {
			//	return;
			//}

			InitializeJobs();
		}

		internal static void InitializeJobs() {
			for (int i = 0; i < JobInitializer.JobCount; i++) {
				InternalJob job = JobInitializer[i];
				Helpers.InBackgroundThread(job.OnJobLoaded);

				if (job.HasJobExpired) {
					job.Dispose();
					JobInitializer.Remove(job.UniqueID);
					continue;
				}

				Logger.Info($"'{job.JobName}' set to fire @ {Math.Round(job.SpanUntilInitialCall.TotalMinutes, 3)} minutes from now {(job.IsRecurring ? $"and {Math.Round(job.DelayBetweenCalls.TotalMinutes, 3)} minutes thereafter." : "")}");
			}
		}

		private static void InitializeJob(InternalJob job) {
			Helpers.InBackgroundThread(job.OnJobLoaded);

			if (job.HasJobExpired) {
				job.Dispose();
				RemoveJob(job.UniqueID);
				return;
			}

			Logger.Info($"'{job.JobName}' set to fire @ {Math.Round(job.SpanUntilInitialCall.TotalMinutes, 3)} minutes from now {(job.IsRecurring ? $"and {Math.Round(job.DelayBetweenCalls.TotalMinutes, 3)} minutes thereafter." : "")}");
		}

		internal static void AddJob(InternalJob job) {
			JobInitializer.Add(job);
			InitializeJob(job);
		}

		internal static InternalJob? GetJob(string uniqueId) => JobInitializer.GetJob(uniqueId);

		internal static void RemoveJob(string uniqueID) => JobInitializer.Remove(uniqueID);
	}
}
