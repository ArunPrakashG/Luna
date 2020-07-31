using Luna.Extensions;
using Luna.Logging;
using Mono.Unix.Native;
using Synergy.Extensions;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.CommandLine {
	internal class CommandProcessSession : IDisposable {
		private const string UNIX_SHELL = "/bin/bash";
		private const string WINDOWS_SHELL = "cmd.exe";
		private readonly InternalLogger Logger;
		private readonly Process Process;
		private readonly bool IsElevationCapable;

		private CancellationTokenSource CommandInputSessionToken = new CancellationTokenSource();
		private readonly ObservableStack<string> OutputContainer;
		private readonly ObservableStack<string> ErrorContainer;
		private readonly ObservableStack<string> InputContainer;

		private string? InputSessionVariable;
		private bool IsInputSessionActive;

		internal CommandProcessSession(string command, bool asAdmin = false) {
			if (string.IsNullOrEmpty(command)) {
				throw new ArgumentNullException(nameof(command));
			}

			string shellName = Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD ? UNIX_SHELL : WINDOWS_SHELL;
			Logger = new InternalLogger(shellName);
			IsElevationCapable = IsElevated();
			command = $"{(Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD ? "-c" : "/C")} \"{EscapeArguments(command)}\"";

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
				FileName = shellName,
				Arguments = command
			};

			Process.EnableRaisingEvents = true;
			Process.Disposed += OnDisposed;
			Process.ErrorDataReceived += OnErrorReceived;
			Process.Exited += OnExit;
			Process.OutputDataReceived += OnOutputReceived;
			Process.Start();
			Process.BeginOutputReadLine();
			Process.BeginErrorReadLine();

			OutputContainer = new ObservableStack<string>();
			ErrorContainer = new ObservableStack<string>();
			InputContainer = new ObservableStack<string>();
			OutputContainer.CollectionChanged += ProcessStandardOutput;
			ErrorContainer.CollectionChanged += ProcessStandardError;
			InputContainer.CollectionChanged += ProcessStandardInput;
			InputSessionVariable = null;
			Helpers.InBackgroundThread(() => InitInputSession(ref InputSessionVariable, CommandInputSessionToken.Token));
		}

		internal void WriteLine(string? data) {
			if (string.IsNullOrEmpty(data) || !IsInputSessionActive) {
				return;
			}

			InputSessionVariable = data;
		}		

		private void ProcessStandardError(object sender, NotifyCollectionChangedEventArgs e) {
			if (!ErrorContainer.TryPop(out string? newLine)) {
				return;
			}


		}

		private void ProcessStandardOutput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!OutputContainer.TryPop(out string? newLine)) {
				return;
			}


		}

		private void ProcessStandardInput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!InputContainer.TryPop(out string? newLine)) {
				return;
			}


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

		private void InitInputSession(ref string? data, CancellationToken cancellationToken) {
			IsInputSessionActive = true;

			while (!cancellationToken.IsCancellationRequested && !Process.HasExited) {
				if (string.IsNullOrEmpty(data)) {
					Task.Delay(1).Wait();
					continue;
				}

				using (StreamWriter inputWriter = Process.StandardInput) {
					inputWriter.WriteLine(data);
					inputWriter.Flush();
				}
			}

			IsInputSessionActive = false;
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

		public void Dispose() {
			if (!CommandInputSessionToken.IsCancellationRequested) {
				CommandInputSessionToken.Cancel();
				CommandInputSessionToken.Dispose();
			}

			Process?.Kill();
			Process?.Dispose();
		}
	}
}
