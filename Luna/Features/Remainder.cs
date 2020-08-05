using Luna.Logging;
using System;
using System.Collections.Generic;

namespace Luna.Features {
	internal class Remainder : InternalJob {
		private readonly InternalLogger Logger;
		private static readonly List<Remainder> Remainders = new List<Remainder>();
		private readonly string Description;

		internal Remainder(string remainderName, string remainderDescription, DateTime scheduledAt) : base(remainderName, scheduledAt) {
			Logger = new InternalLogger(remainderName);
			Description = remainderDescription;
			Remainder.Remainders.Add(this);
		}

		protected override void OnDisposed() {
			for (int i = 0; i < Remainder.Remainders.Count; i++) {
				if (Remainder.Remainders[i].IsDisposed) {
					Remainder.Remainders.RemoveAt(i);
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

		protected override void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			Logger.Info($"REMAINDER > {Description}");

			using(TTS tts = new TTS(false, false)) {
				tts.Speak("Sir, You have a remainder!");
				tts.Speak(Description);
			}
		}
	}
}
