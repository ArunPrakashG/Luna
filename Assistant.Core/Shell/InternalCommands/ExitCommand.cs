using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.InternalCommands {
	public class ExitCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Exit Command";

		public bool IsInitSuccess { get; set; }

		public int MaxParameterCount => 1;

		public string CommandDescription => "Exits assistant process.";

		public string CommandKey => "exit";

		public ShellEnum.COMMAND_CODE CommandCode => ShellEnum.COMMAND_CODE.APP_EXIT;

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

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellOut.Error("Too many arguments.");
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (OnExecuteFunc != null) {
					if (OnExecuteFunc.Invoke(parameter)) {
						return;
					}
				}

				switch (parameter.ParameterCount) {					
					case 0:
						ShellOut.Info("Exiting assistant in 5 seconds...");
						Helpers.ScheduleTask(async () => await Core.Exit(0).ConfigureAwait(false), TimeSpan.FromSeconds(5));
						return;
					case 1 when !string.IsNullOrEmpty(parameter.Parameters[0]):
						if (!int.TryParse(parameter.Parameters[0], out int exitCode)) {
							ShellOut.Error("Couldn't parse exit code argument.");
							return;
						}

						ShellOut.Info($"Exiting assistant with '{exitCode}' exit code in 5 seconds...");
						Helpers.ScheduleTask(async () => await Core.Exit(exitCode).ConfigureAwait(false), TimeSpan.FromSeconds(5));
						return;
					default:
						ShellOut.Error("Command seems to be in incorrect syntax.");
						return;
				}
			}
			catch (Exception e) {
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
				ShellOut.Info($"{CommandName} - {CommandKey} | {CommandDescription} | exit -[exit_code];");
				return;
			}

			ShellOut.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellOut.Info($"|> {CommandDescription}");
			ShellOut.Info($"Basic Syntax -> ' exit; '");
			ShellOut.Info($"Advanced -> ' exit -[exit_code]; '");
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
