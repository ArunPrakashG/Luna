using System;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Shell.InternalCommands {
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

			if (!Program.CoreInstance.IsBaseInitiationCompleted) {
				ShellIO.Error("Cannot execute as the core hasn't been successfully started yet.");
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellIO.Error("Too many arguments.");
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				switch (parameter.ParameterCount) {
					case 0:
					default:
						ShellIO.Info("After this process, you wont be able to execute shell commands in assistant...");
						Interpreter.ExitShell();
						ShellIO.Info("Shutdown process started!");
						return;
				}
			}
			catch (Exception e) {
				ShellIO.Exception(e);
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
				ShellIO.Info($"{CommandName} - {CommandKey} | {CommandDescription} | {CommandKey} ");
				return;
			}

			ShellIO.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellIO.Info($"|> {CommandDescription}");
			ShellIO.Info($"Basic Syntax -> ' {CommandKey} '");
			ShellIO.Info($"----------------- ----------------------------- -----------------");
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
