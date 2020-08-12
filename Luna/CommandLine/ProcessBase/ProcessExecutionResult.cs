using System;

namespace Luna.CommandLine.ProcessBase {
	public sealed class ProcessExecutionResult {
		public readonly int ExitCode;
		public readonly bool IsSuccessExitCode;
		public readonly string StandardError;
		public readonly string StandardOutput;
		public readonly DateTime ExitedAt;

		public ProcessExecutionResult(int exitCode, string stdError, string stdOut, DateTime exitedAt) {
			ExitCode = exitCode;
			IsSuccessExitCode = ExitCode == 0;
			StandardError = stdError;
			StandardOutput = stdOut;
			ExitedAt = exitedAt;
		}
	}
}
