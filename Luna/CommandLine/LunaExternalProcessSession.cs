using Luna.CommandLine.ProcessBase;
using System.Runtime.InteropServices;

namespace Luna.CommandLine {
	internal class LunaExternalProcessSession : CommandProcess {
		internal LunaExternalProcessSession(OSPlatform platform, bool enableIOLogging = false, bool logToInternalTrace = false, bool asAdmin = false) : base(platform, enableIOLogging, logToInternalTrace, asAdmin) {
		}

		internal (string? output, string? error) Run(string command, bool waitForExit) {
			if (string.IsNullOrEmpty(command)) {
				return (null, null);
			}

			using var processSession = new SessionizedProcessBuilder()
				.WithArgument(command)
				.WithCurrentWorkingDirectory()
				.WithNoWindow()
				.ExecuteAsync().Result;

			return ExecuteCommand(command, waitForExit);
		}
	}
}
