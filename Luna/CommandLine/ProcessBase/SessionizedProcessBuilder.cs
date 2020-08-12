using Luna.Logging;
using Mono.Unix.Native;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.CommandLine.ProcessBase {
	public class SessionizedProcessBuilder {
		private const string UNIX_SHELL = "/bin/bash";
		private const string WINDOWS_SHELL = "cmd.exe";

		private readonly string AvailableShell;
		private readonly InternalLogger Logger;
		private readonly bool IsElevationCapable;
		private readonly bool EnableIOLogging;
		private readonly ProcessStartInfo StartInfo;
		private readonly SemaphoreSlim ProcessSemaphore = new SemaphoreSlim(1, 1);
		private readonly List<(RedirectionSource Source, Action<string> RedirectionTarget)> Redirection = new List<(RedirectionSource Source, Action<string> RedirectionTarget)>();

		private Process Process;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		public SessionizedProcessBuilder(string fileName) {
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
			if (string.IsNullOrEmpty(fileName)) {
				throw new ArgumentNullException(nameof(fileName));
			}

			IsElevationCapable = IsElevated();
			AvailableShell = Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD ? UNIX_SHELL : WINDOWS_SHELL;
			Logger = new InternalLogger($"P|{AvailableShell}");
			StartInfo = new ProcessStartInfo() {
				StandardErrorEncoding = Encoding.ASCII,
				StandardOutputEncoding = Encoding.ASCII,
				StandardInputEncoding = Encoding.ASCII,
				FileName = fileName
			};

			StartInfo.RedirectStandardError = true;
			StartInfo.RedirectStandardOutput = true;
			StartInfo.RedirectStandardInput = true;
		}

		public SessionizedProcessBuilder WithArgument(string argument) {
			if (string.IsNullOrEmpty(argument)) {
				throw new ArgumentNullException(nameof(argument));
			}

			bool isUnixEnv = Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD;
			argument = $"{(isUnixEnv ? "-c" : "/C")} {(isUnixEnv && IsElevationCapable ? "sudo" : "")} \"{EscapeArguments(argument)}\"";
			StartInfo.Arguments = argument;
			return this;
		}

		public SessionizedProcessBuilder WithArgument(params string[] argument) {
			if (argument.Length <= 0) {
				throw new ArgumentNullException(nameof(argument));
			}

			bool isUnixEnv = Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD;
			string escaped(string arg) => $"{(isUnixEnv ? "-c" : "/C")} {(isUnixEnv && IsElevationCapable ? "sudo" : "")} \"{EscapeArguments(arg)}\"";

			for (int i = 0; i < argument.Length; i++) {
				if (string.IsNullOrEmpty(argument[i])) {
					continue;
				}

				StartInfo.ArgumentList.Add(escaped(argument[i]));
			}

			return this;
		}

		public SessionizedProcessBuilder WithNoWindow() {
			StartInfo.CreateNoWindow = false;
			StartInfo.UseShellExecute = false;
			StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			return this;
		}

		public SessionizedProcessBuilder WithRedirection(RedirectionSource source, Action<string> redirectTarget) {
			if (redirectTarget == null) {
				throw new ArgumentNullException(nameof(redirectTarget));
			}

			Redirection.Add((source, redirectTarget));
			return this;
		}

		public SessionizedProcessBuilder WithEnvironmentVariable(string variableName, string variableValue) {
			if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(variableValue)) {
				throw new ArgumentNullException(nameof(variableName) + "||" + nameof(variableValue));
			}

			StartInfo.Environment.TryAdd(variableName, variableValue);
			return this;
		}

		public SessionizedProcessBuilder WithEnvironmentVariable(Dictionary<string, string> envVariablePairs) {
			if (envVariablePairs == null || envVariablePairs.Count <= 0) {
				throw new ArgumentNullException(nameof(envVariablePairs));
			}

			envVariablePairs.ForEachElement((k, v) => {
				StartInfo.Environment.TryAdd(k, v);
			}, true);

			return this;
		}

		public SessionizedProcessBuilder WithWorkingDirectoryAs(string directory) {
			if (string.IsNullOrEmpty(directory)) {
				throw new ArgumentNullException(nameof(directory));
			}

			StartInfo.WorkingDirectory = directory;
			return this;
		}

		public SessionizedProcessBuilder WithCurrentWorkingDirectory() {
			StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
			return this;
		}

		public async Task<ProcessExecutionResult> ExecuteAndWaitAsync(TimeSpan waitSpan) {
			Process = new Process();
			RegisterInternalEvents();
			await ProcessSemaphore.WaitAsync().ConfigureAwait(false);
			Process.StartInfo = StartInfo;
			Process.EnableRaisingEvents = true;
			Process.BeginErrorReadLine();
			Process.BeginOutputReadLine();

			Redirection.ForEach((a) => {
				switch (a.Source) {
					case RedirectionSource.StandardError:
						Process.ErrorDataReceived += (s, e) => a.RedirectionTarget.Invoke(e.Data);
						break;
					case RedirectionSource.StandardOutput:
						Process.OutputDataReceived += (s, e) => a.RedirectionTarget.Invoke(e.Data);
						break;
					case RedirectionSource.ProcessExit:
						Process.Exited += (s, e) => a.RedirectionTarget.Invoke(Process.ExitCode.ToString());
						break;
					case RedirectionSource.Disposed:
						Process.Disposed += (s, e) => a.RedirectionTarget.Invoke(string.Empty);
						break;
				}
			});

			Process.Start();
			Process.WaitForExit((int) waitSpan.TotalMilliseconds);
			ProcessSemaphore.Release();
			Process.Dispose();
			return new ProcessExecutionResult(Process.ExitCode, Process.StandardError.ReadToEnd(), Process.StandardOutput.ReadToEnd(), Process.ExitTime);
		}

		public async Task ExecuteAsync() {
			Process = new Process();
			RegisterInternalEvents();
			await ProcessSemaphore.WaitAsync().ConfigureAwait(false);
			Process.StartInfo = StartInfo;
			Process.EnableRaisingEvents = true;
			Process.BeginErrorReadLine();
			Process.BeginOutputReadLine();

			Redirection.ForEach((a) => {
				switch (a.Source) {
					case RedirectionSource.StandardError:
						Process.ErrorDataReceived += (s, e) => a.RedirectionTarget.Invoke(e.Data);
						break;
					case RedirectionSource.StandardOutput:
						Process.OutputDataReceived += (s, e) => a.RedirectionTarget.Invoke(e.Data);
						break;
					case RedirectionSource.ProcessExit:
						break;
					case RedirectionSource.Disposed:
						break;
				}
			});

			Process.Start();
			ProcessSemaphore.Release();
		}

		public void WriteLine(string data) {
			if (Process == null) {
				throw new InvalidOperationException();
			}

			Process.StandardInput.AutoFlush = true;
			Process.StandardInput.WriteLine(data);
		}

		public void WriteLineAsync(Action<StreamWriter> inStream) {
			Helpers.InBackground(async () => {
				while (Process != null && !Process.HasExited) {
					inStream.Invoke(Process.StandardInput);
					await Task.Delay(1).ConfigureAwait(false);
				}
			});
		}

		public ProcessExecutionResult Exit() {
			if (Process == null) {
				throw new InvalidOperationException();
			}

			if (Process.HasExited) {
				return new ProcessExecutionResult(Process.ExitCode, Process.StandardError.ReadToEnd(), Process.StandardOutput.ReadToEnd(), Process.ExitTime);
			}

			Process.Kill();
			return new ProcessExecutionResult(Process.ExitCode, Process.StandardError.ReadToEnd(), Process.StandardOutput.ReadToEnd(), Process.ExitTime);
		}

		public void Dispose() {
			if (Process == null) {
				return;
			}

			if (!Process.HasExited) {
				Process.Kill();
			}

			Process.Dispose();
			ProcessSemaphore.Dispose();
			Redirection.Clear();
		}

		private void RegisterInternalEvents() {
			if(Process == null || Process.HasExited) {
				return;
			}

			Process.ErrorDataReceived += (s, e) => {
				ProcessLog($">>> {e.Data}", ProcessLogLevel.Error);
			};

			Process.OutputDataReceived += (s, e) => {
				ProcessLog($">> {e.Data}", ProcessLogLevel.Info);
			};

			Process.Disposed += (s, e) => {
				ProcessLog($"Process disposed.", ProcessLogLevel.Info);
			};

			Process.Exited += (s, e) => {
				ProcessLog($"{Process.Id} Exited with {Process.ExitCode} exit code.", ProcessLogLevel.Info);
			};
		}

		public enum RedirectionSource {
			StandardError,
			StandardOutput,
			ProcessExit,
			Disposed
		}

		private string EscapeArguments(string command) => command.Replace("\"", "\\\"");

		private static bool IsElevated() {
			if (Helpers.GetPlatform() == OSPlatform.Windows) {
				using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
					WindowsPrincipal principal = new WindowsPrincipal(identity);
					return principal.IsInRole(WindowsBuiltInRole.Administrator);
				}
			}

			if (Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD) {
				return Syscall.geteuid() == 0;
			}

			return false;
		}

		protected void ProcessLog(string? msg, ProcessLogLevel logLevel) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			if (EnableIOLogging) {
				switch (logLevel) {
					case ProcessLogLevel.Error:
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine($"{Process.ProcessName} > {logLevel} > {msg}");
						Console.ResetColor();
						break;
					case ProcessLogLevel.Info:
						Console.WriteLine($"{Process.ProcessName} > {logLevel} > {msg}");
						break;
					case ProcessLogLevel.Input:
						// this can be annoying as it appears when user just passed in an input...
						// Console.WriteLine($"{Process.ProcessName} < {logLevel} > {msg}");
						break;
				}
			}

			Logger.Trace($"{Process.ProcessName} > {logLevel} > {msg}");
		}

		protected enum ProcessLogLevel {
			Info,
			Error,
			Input
		}
	}
}
