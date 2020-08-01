using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class SoxCommandInterfacer : CommandProcess {
		private const string InitiatorCommand = "sox";
		private static readonly OSPlatform SupportedPlatform = OSPlatform.Linux;

		internal SoxCommandInterfacer(bool IOLogging = false, bool internalTraceLogging = false, bool asAdmin = false) : base(SupportedPlatform, IOLogging, internalTraceLogging, asAdmin) { }

		internal void Play(string? fileName) {
			if(string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return;
			}

			ExecuteCommand(GeneratePlayCommand(fileName));
		}

		private string GeneratePlayCommand(string fileName) {
			if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return null;
			}

			return string.Format("{0} {1} {2}", InitiatorCommand, "play", fileName);
		}
	}
}
