using Assistant.Extensions.Shared.Shell;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.InternalCommands {
	public class ShellShutdownCommand : IShellCommand, IDisposable {
		public bool HasParameters => false;

		public string CommandName => "Shutdown shell";

		public bool IsInitSuccess { get; set; }

		public int MaxParameterCount => 0;

		public string CommandDescription => "Exits the assistant shell.";

		public string CommandKey => "quit";

		public SemaphoreSlim Sync { get; set; }
		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public void Dispose() {
			IsInitSuccess = false;
			Sync.Dispose();
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (!Core.CoreInitiationCompleted) {
				ShellOut.Error("Cannot execute as the core hasn't been successfully started yet.");
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellOut.Error("Too many arguments.");
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				switch (parameter.ParameterCount) {
					case 0:
					default:
						ShellOut.Info("After this process, you wont be able to execute shell commands in assistant...");
						Interpreter.ShutdownShell = true;
						ShellOut.Info("Shutdown process started!");
						return;
				}
			}
			catch(Exception e) {
				ShellOut.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		public async Task InitAsync() {
			Sync = new SemaphoreSlim(1, 1);
			IsInitSuccess = true;
		}

		public void OnHelpExec(bool quickHelp) {
			if (quickHelp) {
				ShellOut.Info($"{CommandName} - {CommandKey} | {CommandDescription} | {CommandKey} ");
				return;
			}

			ShellOut.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellOut.Info($"|> {CommandDescription}");
			ShellOut.Info($"Basic Syntax -> ' {CommandKey} '");
			ShellOut.Info($"----------------- ----------------------------- -----------------");
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
