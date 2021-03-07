using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Luna.CommandLine.ProcessBase {
	public class ProcessSession : IDisposable {
		private readonly Process Process;
		private readonly SessionizedProcessBuilder SessionBuilder;
		private StreamWriter InputStreamWriter => Process.StandardInput;
		private StreamReader OutputStreamReader => Process.StandardOutput;
		private StreamReader ErrorStreamReader => Process.StandardError;

		internal ProcessSession(SessionizedProcessBuilder sessionBuilder, Process process) {
			SessionBuilder = sessionBuilder ?? throw new ArgumentNullException(nameof(sessionBuilder));
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Process.StandardInput.AutoFlush = true;
		}

		public async Task Write(string data) {
			if (string.IsNullOrEmpty(data)) {
				return;
			}

			await InputStreamWriter.WriteAsync(data).ConfigureAwait(false);
		}

		public async Task WriteLine(string data) {
			if (string.IsNullOrEmpty(data)) {
				return;
			}

			await InputStreamWriter.WriteLineAsync(data).ConfigureAwait(false);
		}

		public async Task<SessionOut?> WriteWithResultAsync(string data) {
			if (string.IsNullOrEmpty(data)) {
				return null;
			}

			bool isWaitingForResult = false;
			Process.OutputDataReceived += (s, e) => { isWaitingForResult = false; };
			Process.ErrorDataReceived += (s, e) => { isWaitingForResult = false; };

			await InputStreamWriter.WriteLineAsync(data).ConfigureAwait(false);
			isWaitingForResult = true;

			if (isWaitingForResult) {
				await Task.Delay(1).ConfigureAwait(false);
			}

			return new SessionOut(
				await OutputStreamReader.ReadToEndAsync().ConfigureAwait(false),
				await ErrorStreamReader.ReadToEndAsync().ConfigureAwait(false)
			);
		}

		public void Dispose() {
			SessionBuilder.Dispose();
		}
	}
}
