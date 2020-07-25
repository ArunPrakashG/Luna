using FluentScheduler;
using Luna.Logging;
using Luna.Rest;
using System;
using System.Collections.Generic;

namespace Luna {
	internal static class InternalEvents {
		private static readonly InternalLogger Logger = new InternalLogger(nameof(InternalEvents));

		static InternalEvents() {
			JobManager.JobException += JobManagerOnException;
			JobManager.JobStart += JobManagerOnJobStart;
			JobManager.JobEnd += JobManagerOnJobEnd;
		}

		private static void JobManagerOnException(JobExceptionInfo obj) => Logger.Exception(obj.Exception);

		private static void JobManagerOnJobEnd(JobEndInfo obj) {
			if (obj.Name.Equals("ConsoleUpdater", StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			Logger.Trace($"A job has ended -> {obj.Name} / {obj.StartTime.ToString()}");
		}

		private static void JobManagerOnJobStart(JobStartInfo obj) {
			if (obj.Name.Equals("ConsoleUpdater", StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			Logger.Trace($"A job has started -> {obj.Name} / {obj.StartTime.ToString()}");
		}
	}
}
