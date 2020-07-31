using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Features {
	internal class GenericInternalJob : InternalJob {
		private readonly InternalLogger Logger;
		private readonly Action<ObjectParameterWrapper?> OnTriggered;
		private readonly Action? OnLoaded;
		private readonly Action? OnInitialized;
		private readonly ObjectParameterWrapper? Parameters;

		private static readonly List<GenericInternalJob> GenericInternalJobs = new List<GenericInternalJob>();

		internal GenericInternalJob(string jobName, DateTime scheduledAt, Action<ObjectParameterWrapper?> onTriggeredAction, Action? onLoaded = null, Action? onInitialized = null, ObjectParameterWrapper? parameters = null) : base(jobName, scheduledAt, parameters) {
			Logger = new InternalLogger(jobName ?? throw new ArgumentNullException(nameof(jobName)));
			OnTriggered = onTriggeredAction ?? throw new ArgumentNullException(nameof(onTriggeredAction));
			OnLoaded = onLoaded;
			OnInitialized = onInitialized;
			Parameters = parameters;
			
			GenericInternalJob.GenericInternalJobs.Add(this);
		}

		internal GenericInternalJob(string jobName, TimeSpan scheduledSpan, Action<ObjectParameterWrapper?> onTriggeredAction, Action? onLoaded = null, Action? onInitialized = null, ObjectParameterWrapper? parameters = null) : base(jobName, scheduledSpan, parameters) {
			Logger = new InternalLogger(jobName ?? throw new ArgumentNullException(nameof(jobName)));
			OnTriggered = onTriggeredAction ?? throw new ArgumentNullException(nameof(onTriggeredAction));
			OnLoaded = onLoaded;
			OnInitialized = onInitialized;
			Parameters = parameters;

			GenericInternalJob.GenericInternalJobs.Add(this);
		}

		protected override void OnJobInitialized() {
			OnInitialized?.Invoke();
		}

		protected override void OnJobLoaded() {
			OnLoaded?.Invoke();
		}

		protected override void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			OnTriggered.Invoke(Parameters);
		}

		protected override void OnDisposed() {
			for(int i = 0; i < GenericInternalJob.GenericInternalJobs.Count; i++) {
				if (GenericInternalJob.GenericInternalJobs[i].IsDisposed) {
					GenericInternalJob.GenericInternalJobs.RemoveAt(i);
				}
			}
		}
	}
}
