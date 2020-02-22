using Assistant.Extensions.Shared.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.Commands {
	public class HelpCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Help Command";

		public string CommandKey => "help";

		public string CommandDescription => "Displays information of the specified command.";

		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public int MaxParameterCount => 1;
		public ShellEnum.COMMAND_CODE CommandCode => ShellEnum.COMMAND_CODE.HELP_ADVANCED;

		public bool IsInitSuccess { get; set; }

		private SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		public void Dispose() {
			IsInitSuccess = false;
			Sync.Dispose();
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (parameter.Parameters == null || parameter.Parameters.Length <= 0) {
				ShellOut.Error("Invalid Arguments.");
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

				string? helpCmdKey = parameter.Parameters[0];

				if (string.IsNullOrEmpty(helpCmdKey)) {
					ShellOut.Error("Argument is invalid.");
					return;
				}

				if (helpCmdKey.Equals("all", StringComparison.OrdinalIgnoreCase)) {
					PrintAll();
					return;
				}

				IShellCommand command = await Interpreter.Init.GetCommandWithKeyAsync<IShellCommand>(helpCmdKey).ConfigureAwait(false);

				if (command == null) {
					ShellOut.Error("Command doesn't exist. use 'help' to check all available commands!");
					return;
				}

				ShellOut.Info("--------------------------------------- Command ---------------------------------------");
				ShellOut.Info($"Command Key -> {command.CommandKey}");

				if (string.IsNullOrEmpty(command.CommandName)) {
					ShellOut.Error("Command name isn't set. Who made this command anyway ?");
					return;
				}

				ShellOut.Info($"Command Name -> {command.CommandName}");

				if (string.IsNullOrEmpty(command.CommandDescription)) {
					ShellOut.Error("Command description isn't set. Come on...");
					return;
				}

				ShellOut.Info($"Command Description -> {command.CommandDescription}");
				ShellOut.Info("---------------------------------------------------------------------------------------");
			}catch(Exception e) {
				ShellOut.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		private void PrintAll() {
			if (Interpreter.CommandsCount <= 0) {
				ShellOut.Error("No commands exist.");
				return;
			}

			ShellOut.Info("--------------------------------------- Commands ---------------------------------------");
			foreach (KeyValuePair<string, IShellCommand> cmd in Interpreter.Commands) {
				if (string.IsNullOrEmpty(cmd.Key) || cmd.Value == null) {
					continue;
				}

				ShellOut.Info($"Command Key -> {cmd.Value.CommandKey}");
				ShellOut.Info($"Command Name -> {cmd.Value.CommandName}");
				ShellOut.Info($"Command Description -> {cmd.Value.CommandDescription}");
				ShellOut.Info("******************************************************");
			}
			ShellOut.Info("---------------------------------------------------------------------------------------");
		}

		public async Task InitAsync() {
			Sync = new SemaphoreSlim(1, 1);
			IsInitSuccess = true;
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
