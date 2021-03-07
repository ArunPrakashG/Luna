using Luna.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Luna.Features {
	internal class PeriodicTempRemover : InternalJob {
		private readonly InternalLogger Logger;

		internal PeriodicTempRemover(TimeSpan scheduledSpan, ObjectParameterWrapper? parameters = null) : base(nameof(PeriodicTempRemover), scheduledSpan, parameters) {
			Logger = new InternalLogger(nameof(PeriodicTempRemover));
			this.ReoccureEvery(scheduledSpan);
		}

		protected override void OnDisposed() {
			
		}

		protected override void OnJobInitialized() {
			
		}

		protected override void OnJobLoaded() {
			
		}

		protected override void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			string tempPath = Path.GetTempPath();

			if (!Directory.Exists(tempPath)) {
				return;
			}

			string[] files = Directory.GetFiles(tempPath, "*mp3");

			if(files.Length <= 0) {
				return;
			}

			Logger.Info($"Clearing {files.Length} temp files...");
			for(int i = 0; i < files.Length; i++) {
				if (!File.Exists(files[i])) {
					continue;
				}

				File.Delete(files[i]);
			}

			Logger.Info($"Cleared {files.Length} files.");
		}
	}
}
