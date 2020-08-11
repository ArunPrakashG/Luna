using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class LunaExternalProcessSession : CommandProcess {
		internal LunaExternalProcessSession(OSPlatform platform, bool enableIOLogging = false, bool logToInternalTrace = false, bool asAdmin = false) : base(platform, enableIOLogging, logToInternalTrace, asAdmin) {
		}

		internal (string? output, string? error) Run(string command, bool waitForExit) {
			if (string.IsNullOrEmpty(command)) {
				return (null, null);
			}

			return ExecuteCommand(command, waitForExit);			
		}
	}
}
