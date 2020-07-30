using FluentScheduler;
using Luna.Logging;
using Luna.Sound.Speech;
using System;

namespace Luna.Features.Remainders {
	internal class Remainder : InternalJob {
		private readonly InternalLogger Logger = new InternalLogger(nameof(Remainder));
		private readonly string Description;

		internal Remainder(string remainderName, string remainderDescription, DateTime scheduledAt) : base(remainderName, scheduledAt) {
			Description = remainderDescription;
		}

		internal override void OnJobInitialized() {
			// called right after OnJobLoaded
			Logger.Trace($"EVENT -> {nameof(OnJobInitialized)}");
		}

		internal override void OnJobLoaded() {
			// called when job is loaded
			Logger.Trace($"EVENT -> {nameof(OnJobLoaded)}");
		}

		internal override async void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			await TTS.SpeakText(Description, false).ConfigureAwait(false);
		}
	}
}
