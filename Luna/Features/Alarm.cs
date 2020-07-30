using Luna.Logging;
using Luna.Sound.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Luna.Features {
	internal class Alarm : InternalJob {
		private readonly string Description;
		private readonly bool UseTTS;

		internal static readonly List<Alarm> Alarms = new List<Alarm>();
		internal static readonly InternalLogger Logger = new InternalLogger(nameof(Alarm));

		internal Alarm(string alarmDescription, bool useTts, DateTime scheduledAt, string alarmName, TimeSpan delayBetweenCalls) : base(alarmName, scheduledAt, delayBetweenCalls) {
			Description = alarmDescription;			
			UseTTS = useTts;

			if(Alarms.Where(x => x.UniqueID.Equals(this.UniqueID)).Count() <= 0) {
				Alarms.Add(this);
			}
		}

		internal override async void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			Logger.Trace($"Alarm event -> {this.UniqueID}");
			// play alarm notification sound
			// read console or wait for an input to snooze or cancel alarm

			if (UseTTS) {
				await TTS.SpeakText(Description, false).ConfigureAwait(false);
			}
		}

		internal override void OnJobInitialized() {
			// called right after OnJobLoaded
			Logger.Trace($"EVENT -> {nameof(OnJobInitialized)}");
		}

		internal override void OnJobLoaded() {
			// called when job is loaded
			Logger.Trace($"EVENT -> {nameof(OnJobLoaded)}");
		}
	}
}
