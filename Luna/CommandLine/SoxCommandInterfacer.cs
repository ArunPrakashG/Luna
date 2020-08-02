using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.CommandLine {
	internal class SoxCommandInterfacer : CommandProcess {
		private const string InitiatorCommand = "sox";
		private static readonly OSPlatform SupportedPlatform = OSPlatform.Linux;
		private static CancellationTokenSource PlayToken = new CancellationTokenSource();

		internal SoxCommandInterfacer(bool IOLogging = false, bool internalTraceLogging = false, bool asAdmin = false) : base(SupportedPlatform, IOLogging, internalTraceLogging, asAdmin) { }

		internal void Play(string? fileName) {
			if(string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return;
			}

			ExecuteCommand(GeneratePlayCommand(fileName));
		}

		internal async Task PlayAsync(string? fileName) {
			if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return;
			}

			if(PlayToken != null) {
				PlayToken.Cancel();
				PlayToken.Dispose();
			}
			
			PlayToken = new CancellationTokenSource();
			await Task.Run(() => ExecuteCommand(GeneratePlayCommand(fileName)), PlayToken.Token).ConfigureAwait(false);
		}

		private string GeneratePlayCommand(string fileName) {
			if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return null;
			}

			return string.Format("{0} {1} {2}", InitiatorCommand, "play", fileName);
		}
	}
}
