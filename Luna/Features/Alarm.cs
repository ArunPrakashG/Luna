using Luna.Logging;
using System;
using System.Collections.Generic;

namespace Luna.Features {
	internal class Alarm : InternalJob {
		private readonly InternalLogger Logger;
		private readonly string Description;
		private readonly bool UseTTS;

		private static readonly List<Alarm> Alarms = new List<Alarm>();

		internal Alarm(string alarmDescription, bool useTts, DateTime scheduledAt, string alarmName) : base(alarmName ?? throw new ArgumentNullException(nameof(alarmName)), scheduledAt) {
			Logger = new InternalLogger(nameof(alarmName));
			Description = alarmDescription;
			UseTTS = useTts;
			Alarm.Alarms.Add(this);
		}

		protected override void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			Logger.Trace($"Alarm event -> {this.UniqueID}");
			// play alarm notification sound
			// read console or wait for an input to snooze or cancel alarm

			if (UseTTS) {
				using(TTS tts = new TTS()) {
					tts.Speak(Description);
				}
			}
		}

		protected override void OnJobInitialized() {
			// called right after OnJobLoaded
			Logger.Trace($"EVENT -> {nameof(OnJobInitialized)}");
		}

		protected override void OnJobLoaded() {
			// called when job is loaded
			Logger.Trace($"EVENT -> {nameof(OnJobLoaded)}");
		}

		protected override void OnDisposed() {
			for (int i = 0; i < Alarm.Alarms.Count; i++) {
				if (Alarm.Alarms[i].IsDisposed) {
					Alarm.Alarms.RemoveAt(i);
				}
			}
		}
	}
}
