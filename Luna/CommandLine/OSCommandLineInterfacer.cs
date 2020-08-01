using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class OSCommandLineInterfacer : CommandProcess {
		internal OSCommandLineInterfacer(OSPlatform platform, bool IOLogging = false, bool traceOnly = false, bool asAdmin = false) : base(platform, IOLogging, traceOnly, asAdmin) {	}

		internal void Execute(string command) {
			if (string.IsNullOrEmpty(command)) {
				return;
			}

			ExecuteCommand(command);
		}
	}
}
