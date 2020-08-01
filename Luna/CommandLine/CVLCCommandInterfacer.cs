using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class CVLCCommandInterfacer : CommandProcess {
		private const string InitiatorCommand = "cvlc";
		private static readonly OSPlatform SupportedPlatform = OSPlatform.Linux;

		internal CVLCCommandInterfacer(bool IOLogging = false, bool traceOnly = false, bool asAdmin = false) : base(SupportedPlatform, IOLogging, traceOnly, asAdmin) {}

		internal void Play(string fileNameOrDirPath) {
			if (string.IsNullOrEmpty(fileNameOrDirPath)) {
				return;
			}

			if (Directory.Exists(fileNameOrDirPath) && Directory.GetFiles(fileNameOrDirPath).Length > 0) {
				ExecuteCommand(GeneratePlayCommand(fileNameOrDirPath));
				return;
			}

			if (!File.Exists(fileNameOrDirPath)) {
				return;
			}

			ExecuteCommand(GeneratePlayCommand(fileNameOrDirPath));
		}

		private string GeneratePlayCommand(string fileName) {
			if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return null;
			}

			return string.Format("{0} {1}", InitiatorCommand, fileName);
		}
	}
}
