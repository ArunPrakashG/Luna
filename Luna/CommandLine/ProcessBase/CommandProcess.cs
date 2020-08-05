using Luna.Extensions;
using Luna.ExternalExtensions;
using Luna.Logging;
using Mono.Unix.Native;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Luna.CommandLine.ProcessBase {
	internal abstract class CommandProcess : IDisposable {
		private const string UNIX_SHELL = "/bin/bash";
		private const string WINDOWS_SHELL = "cmd.exe";
		private readonly string ShellName = Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD ? UNIX_SHELL : WINDOWS_SHELL;
		private readonly InternalLogger Logger;

		protected readonly bool IsElevationCapable;
		protected readonly bool EnableIOLogging;
		protected readonly bool InternalTraceLogging;
		protected readonly ObservableStack<string> OutputContainer;
		protected readonly ObservableStack<string> ErrorContainer;
		protected readonly ObservableStack<string> InputContainer;

		private Process Process;

		protected bool IsUnixEnvironment => Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD;

		internal CommandProcess(OSPlatform platform, bool enableIOLogging = false, bool logToInternalTrace = false, bool asAdmin = false) {
			if (platform != Helpers.GetPlatform()) {
				throw new PlatformNotSupportedException();
			}

			InternalTraceLogging = logToInternalTrace;
			EnableIOLogging = enableIOLogging;
			Logger = new InternalLogger(ShellName);
			IsElevationCapable = IsElevated();
			GenerateProcessInstance();
			OutputContainer = new ObservableStack<string>();
			ErrorContainer = new ObservableStack<string>();
			InputContainer = new ObservableStack<string>();
			OutputContainer.CollectionChanged += ProcessStandardOutput;
			ErrorContainer.CollectionChanged += ProcessStandardError;
			InputContainer.CollectionChanged += ProcessStandardInput;
		}

		protected virtual void GenerateProcessInstance() {
			if (Process != null) {
				if (!Process.HasExited) {
					Process.Kill();
				}

				Process.Dispose();
			}

			Process = new Process();
			Process.StartInfo = new ProcessStartInfo() {
				CreateNoWindow = false,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				StandardErrorEncoding = Encoding.ASCII,
				StandardOutputEncoding = Encoding.ASCII,
				StandardInputEncoding = Encoding.ASCII,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = Directory.GetCurrentDirectory(),
				FileName = ShellName
			};

			Process.EnableRaisingEvents = true;
			Process.Disposed += OnDisposed;
			Process.ErrorDataReceived += OnErrorReceived;
			Process.Exited += OnExit;
			Process.OutputDataReceived += OnOutputReceived;
		}

		protected void ExecuteCommand(string? command) {
			if (Process == null) {
				GenerateProcessInstance();
			}

			if (string.IsNullOrEmpty(command)) {
				return;
			}

			command = $"{(IsUnixEnvironment ? "-c" : "/C")} {(IsUnixEnvironment && IsElevationCapable ? "sudo" : "")} \"{EscapeArguments(command)}\"";
			Process.StartInfo.Arguments = command;
			Process.Start();
			Process.BeginOutputReadLine();
			Process.BeginErrorReadLine();
		}

		protected virtual void ProcessStandardError(object sender, NotifyCollectionChangedEventArgs e) {
			if (!ErrorContainer.TryPop(out string? newLine)) {
				return;
			}

			ProcessLog(newLine, ProcessLogLevel.Error);
		}

		protected virtual void ProcessStandardOutput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!OutputContainer.TryPop(out string? newLine)) {
				return;
			}

			ProcessLog(newLine, ProcessLogLevel.Info);
		}

		protected virtual void ProcessStandardInput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!InputContainer.TryPop(out string? newLine)) {
				return;
			}

			ProcessLog(newLine, ProcessLogLevel.Input);
		}

		private void OnOutputReceived(object sender, DataReceivedEventArgs e) {
			if ((sender == null) || (e == null) || (string.IsNullOrEmpty(e.Data))) {
				return;
			}

			OutputContainer.Push(e.Data);
		}

		private void OnExit(object? sender, EventArgs e) {
			if ((sender == null) || (e == null)) {
				return;
			}

			string fullOutput = Process.StandardOutput.ReadToEnd();
			string fullError = Process.StandardError.ReadToEnd();
			Logger.Info(fullOutput);
			Logger.Info(fullError);

			Dispose();
		}

		private void OnErrorReceived(object sender, DataReceivedEventArgs e) {
			if ((sender == null) || (e == null) || (string.IsNullOrEmpty(e.Data))) {
				return;
			}

			ErrorContainer.Push(e.Data);
		}

		private void OnDisposed(object? sender, EventArgs e) {
			if ((sender == null) || (e == null)) {
				return;
			}

			Dispose();
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

			if (InternalTraceLogging) {
				Logger.Trace($"{Process.ProcessName} > {logLevel} > {msg}");
			}
		}

		protected enum ProcessLogLevel {
			Info,
			Error,
			Input
		}

		public void Dispose() {
			if (Process != null) {
				if (!Process.HasExited) {
					Process.Kill();
				}

				Process.Dispose();
			}

			OutputContainer.Clear();
			ErrorContainer.Clear();
			InputContainer.Clear();
		}
	}
}
