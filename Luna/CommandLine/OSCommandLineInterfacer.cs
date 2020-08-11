using Luna.CommandLine.ProcessBase;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	public class OSCommandLineInterfacer : CommandProcess {

		public OSCommandLineInterfacer(OSPlatform platform, bool IOLogging = false, bool traceOnly = false, bool asAdmin = false) : base(platform, IOLogging, traceOnly, asAdmin) {	}

		public void Execute(string command) {
			if (string.IsNullOrEmpty(command)) {
				return;
			}

			ExecuteCommand(command);
		}

		public string ExecuteDirect(string command) {
			if (Helpers.GetPlatform() != OSPlatform.Linux || string.IsNullOrEmpty(command)) {
				return null;
			}

			string escapedArgs = command.Replace("\"", "\\\"");
			string args = $"-c \"{escapedArgs}\"";

			using Process process = new Process() {
				StartInfo = new ProcessStartInfo {
					FileName = "/bin/bash",
					Arguments = args,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				}
			};

			StringBuilder result = new StringBuilder();

			if (process.Start()) {
				result.AppendLine(process.StandardOutput.ReadToEnd());
				result.AppendLine(process.StandardError.ReadToEnd());
				process.WaitForExit(TimeSpan.FromMinutes(6).Milliseconds);
			}

			return result.ToString();
		}

		protected override void ProcessStandardOutput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!OutputContainer.TryPop(out string? newLine)) {
				return;
			}

			ProcessLog(newLine, ProcessLogLevel.Info);
		}
	}
}
