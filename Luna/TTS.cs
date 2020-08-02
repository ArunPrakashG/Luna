using Luna.CommandLine;
using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luna {
	internal class TTS : IDisposable {
		private readonly InternalLogger Logger = new InternalLogger(nameof(TTS));
		private readonly FestivalCommandSession FestivalSession;

		internal TTS(bool ioLogging = false, bool asAdmin = false) {
			FestivalSession = new FestivalCommandSession(ioLogging, asAdmin);
		}

		internal void Speak(string? text) {
			if (string.IsNullOrEmpty(text)) {
				return;
			}

			FestivalSession.SayText(text);
		}

		public void Dispose() {
			FestivalSession?.Dispose();
		}
	}
}
