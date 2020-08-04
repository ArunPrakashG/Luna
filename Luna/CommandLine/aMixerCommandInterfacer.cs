using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class aMixerCommandInterfacer : CommandProcess {
		private const string InitiatorCommand = "amixer";
		private static readonly OSPlatform SupportedPlatform = OSPlatform.Linux;

		internal aMixerCommandInterfacer(bool enableIOLogging = false, bool logToInternalTrace = false, bool asAdmin = false) : base(SupportedPlatform, enableIOLogging, logToInternalTrace, asAdmin) {
		}

		private string GenerateSetVolumnCommand(int percent) {
			if(percent < 0) {
				return null;
			}

			return string.Format("{0} {1} '{2}' {3}%", InitiatorCommand, "set", "Speaker", percent);
		}

		internal void SetVolumn(int percent) {
			if (percent < 0) {
				return;
			}

			ExecuteCommand(GenerateSetVolumnCommand(percent));
		}
	}
}
