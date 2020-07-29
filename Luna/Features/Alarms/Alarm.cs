using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Luna.Features.Alarms {
	internal class Alarm : InternalJobBase {
		private readonly string Description;
		private readonly bool UseTTS;

		internal Alarm(string jobDescription, bool useTts, DateTime scheduledAt, string jobName, TimeSpan delayBetweenCalls) : base(jobName, scheduledAt, delayBetweenCalls) {
			Description = jobDescription;			
			UseTTS = useTts;
		}

		internal void SetAlarm() {

		}

		internal override void OnJobOccured(ObjectParameterWrapper? objectParameterWrapper) {

		}

		internal override void OnJobInitialized() {

		}

		internal override void OnJobLoaded() {

		}
	}
}
