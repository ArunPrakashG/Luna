namespace Luna.Shell {
	using Luna.ExternalExtensions;
	using Luna.ExternalExtensions.Shared.Shell;
	using Luna.Logging;
	using Luna.Logging.Interfaces;
	using Luna.Shell.InternalCommands;
	using Luna.Sound;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using Parameter = ExternalExtensions.Shared.Shell.Parameter;

	/// <summary>
	/// The Shell Instance.
	/// </summary>
	public static class Interpreter {
		private static readonly ILogger Logger = new Logger("INTERPRETER");
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim LoopSync = new SemaphoreSlim(1, 1);
		private static bool InitCompleted = false;
		internal static bool PauseShell { get; private set; } = false;

		/// <summary>
		/// The external command loader instance.<br>Used to load commands onto shell instance.</br>
		/// </summary>
		internal static readonly CommandInitializer Init = new CommandInitializer();

		/// <summary>
		/// Contains all the commands currently loaded into the shell.
		/// </summary>
		internal static readonly Dictionary<string, IShellCommand> Commands = new Dictionary<string, IShellCommand>();

		/// <summary>
		/// Positive value will start shutdown process on the shell.
		/// </summary>
		private static bool ShutdownShell = false;

		/// <summary>
		/// Gets the CurrentCommand
		/// The current command which is being executed.
		/// </summary>
		public static string? CurrentCommand { get; private set; }

		/// <summary>
		/// Gets the CommandsCount
		/// The current usable commands count.
		/// </summary>
		public static int CommandsCount => Commands.Count;

		/// <summary>
		/// Initializes static members of the <see cref="Interpreter"/> class.
		/// </summary>
		static Interpreter() {
			if (!Directory.Exists(Constants.COMMANDS_PATH)) {
				Directory.CreateDirectory(Constants.COMMANDS_PATH);
			}
		}

		/// <summary>
		/// Loads the internally defined shell commands, then loads the external commands libraries, then starts the shell instance.
		/// </summary>		
		/// <returns>Boolean, indicating if the process was successful.</returns>
		public static async Task<bool> InitInterpreterAsync() {
			if (InitCompleted) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				await Init.LoadInternalCommandsAsync<IShellCommand>().ConfigureAwait(false);
				await Init.LoadCommandsAsync<IShellCommand>().ConfigureAwait(false);

				InitCompleted = true;
				Helpers.InBackground(async () => await ReplAsync().ConfigureAwait(false));
				return InitCompleted;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}

		internal static void Pause() => PauseShell = true;

		internal static void Resume() => PauseShell = false;

		internal static void ExitShell() => ShutdownShell = true;

		/// <summary>
		/// The REPL Loop. <br>Loops until an external shutdown of the shell as been triggered by the assistant core program.</br>
		/// </summary>
		/// <returns></returns>
		private static async Task ReplAsync() {
			if (!InitCompleted) {
				return;
			}

			await LoopSync.WaitAsync().ConfigureAwait(false);
			try {
				Logger.Info("Command Shell has been loaded!");
				bool isDisplayed = false;

				do {
					if (PauseShell) {
						if (!isDisplayed) {
							Logger.Trace("Shell is in 'Paused' state.");
							isDisplayed = true;
						}

						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					Logger.Trace("Shell is in 'Running' state.");
					Console.WriteLine();
					ShellOut.Info("Type in the command! Use 'help' / 'h' for help regarding the available commands.");
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write($"#~{Path.GetFullPath(Assembly.GetExecutingAssembly().Location)}/{Program.CoreInstance.AssistantName.Trim()}/$ >> ");
					Console.ResetColor();
					string command = Console.ReadLine();

					if (string.IsNullOrEmpty(command) || string.IsNullOrWhiteSpace(command)) {
						ShellOut.Error("Invalid command.");
						continue;
					}

					await ExecuteCommandAsync(command).ConfigureAwait(false);
					Console.WriteLine("Completed!");
					Pause();
				} while (!ShutdownShell);
			}
			catch (Exception e) {
				Logger.Exception(e);
				Logger.Error("Fatal exception has occurred internally. Shell cannot continue.");
				return;
			}
			finally {
				LoopSync.Release();
				Logger.Info("Shell has been shutdown.");
			}
		}

		/// <summary>
		/// Executes the received command by first parsing it then executing with the parsed values.
		/// </summary>
		/// <param name="command">The command<see cref="string"/></param>
		/// <returns>The <see cref="bool"/></returns>
		private static async Task<bool> ExecuteCommandAsync(string? command) {
			if (!InitCompleted) {
				Logger.Warning("Shell isn't initiated properly.");
				ShellOut.Error("Shell is offline!");
				return false;
			}

			if (string.IsNullOrEmpty(command)) {
				Logger.Trace("Command is null.");
				ShellOut.Error("Command empty or invalid.");
				return false;
			}

			CurrentCommand = command;
			return await ParseCommandAsync(command).ConfigureAwait(false);
		}

		/// <summary>
		/// Parses the raw command string value. <br>Then executes it by calling the Execute() method with the parsed values.</br>
		/// </summary>
		/// <param name="cmd">The command <see cref="string"/></param>
		/// <returns>The <see cref="bool"/></returns>
		private static async Task<bool> ParseCommandAsync(string cmd) {
			if (Commands.Count <= 0) {
				ShellOut.Info("No commands have been loaded into the shell.");
				return false;
			}

			if (string.IsNullOrEmpty(cmd)) {
				ShellOut.Error("Invalid command.");
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			bool anyExec = false;

			try {
				// If there is multiple commands separated by the Multi command delimiter
				if (cmd.Contains(ShellConstants.MULTI_COMMAND)) {
					//commands - returns {help -argument}
					string[] cmdSplit = cmd.Split(ShellConstants.MULTI_COMMAND, StringSplitOptions.RemoveEmptyEntries);

					if (cmdSplit == null || cmdSplit.Length <= 0) {
						ShellOut.Error("Failed to parse the command. Please retype in correct syntax!");
						return false;
					}

					// for each -> {help -arg1 -arg2}
					for (int i = 0; i < cmdSplit.Length; i++) {
						if (string.IsNullOrEmpty(cmdSplit[i])) {
							continue;
						}

						bool doesContainArgs = cmdSplit[i].Contains(ShellConstants.ARGUMENT_SPLITTER);
						string[] parameters = new string[0];
						string? commandKey = null;

						// If contains arguments
						if (doesContainArgs) {
							string[] cmdArgumentSplit = cmdSplit[i].Split(ShellConstants.ARGUMENT_SPLITTER, StringSplitOptions.RemoveEmptyEntries);
							if (cmdArgumentSplit == null || cmdArgumentSplit.Length <= 0) {
								continue;
							}

							parameters = new string[cmdArgumentSplit.Length - 1];

							for (int k = 0; k < cmdArgumentSplit.Length; k++) {
								if (string.IsNullOrEmpty(cmdArgumentSplit[k])) {
									continue;
								}

								cmdArgumentSplit[k] = Replace(cmdArgumentSplit[k].Trim());

								if (string.IsNullOrEmpty(commandKey)) {
									commandKey = cmdArgumentSplit[k];
								}

								if (cmdArgumentSplit[k].Equals(commandKey, StringComparison.OrdinalIgnoreCase)) {
									continue;
								}

								parameters[k - 1] = cmdArgumentSplit[k];
							}
						}
						else {
							// If no arguments
							commandKey = Replace(cmdSplit[i].Trim());
						}

						if (string.IsNullOrEmpty(commandKey)) {
							continue;
						}

						if (await Execute(commandKey, parameters).ConfigureAwait(false))
							anyExec = true;
						continue;
					}
				}
				else {
					//If there is only single command
					bool doesContainArgs = cmd.Contains(ShellConstants.ARGUMENT_SPLITTER);
					string[] parameters = new string[0];
					string? commandKey = null;

					if (doesContainArgs) {
						string[] cmdArgumentSplit = cmd.Split(ShellConstants.ARGUMENT_SPLITTER, StringSplitOptions.RemoveEmptyEntries);

						if (cmdArgumentSplit == null || cmdArgumentSplit.Length <= 0) {
							return false;
						}

						parameters = new string[cmdArgumentSplit.Length - 1];

						for (int k = 0; k < cmdArgumentSplit.Length; k++) {
							if (string.IsNullOrEmpty(cmdArgumentSplit[k])) {
								continue;
							}

							cmdArgumentSplit[k] = Replace(cmdArgumentSplit[k].Trim());

							if (string.IsNullOrEmpty(commandKey)) {
								commandKey = cmdArgumentSplit[k];
							}

							if (cmdArgumentSplit[k].Equals(commandKey, StringComparison.OrdinalIgnoreCase)) {
								continue;
							}

							parameters[k - 1] = cmdArgumentSplit[k];
						}
					}
					else {
						commandKey = Replace(cmd.Trim());
					}

					if (string.IsNullOrEmpty(commandKey)) {
						return false;
					}

					if (await Execute(commandKey, parameters).ConfigureAwait(false))
						anyExec = true;
				}

				if (!anyExec) {
					return false;
				}

				return true;
			}
			catch (Exception e) {
				Logger.Log(e);
				ShellOut.Error("Internal exception occurred. Execution failed unexpectedly.");
				ShellOut.Exception(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}

		/// <summary>
		/// Executes the command.
		/// Searches the command with its command key on the internal command collection.
		/// Cross checks the result command by verifying its argument count.
		/// Then executes the command.
		/// </summary>
		/// <param name="commandKey">The command key</param>
		/// <param name="parameters">The parameters</param>
		/// <returns>Boolean indicating status of the execution</returns>
		private static async Task<bool> Execute(string commandKey, string[] parameters) {
			IShellCommand command = await Init.GetCommandWithKeyAsync<IShellCommand>(commandKey).ConfigureAwait(false);

			if (command == null) {
				ShellOut.Error("Command doesn't exist. use 'help' to check all available commands!");
				return false;
			}

			try {
				if (!command.IsInitSuccess) {
					await command.InitAsync().ConfigureAwait(false);
				}

				if (!command.IsCurrentCommandContext(command.CommandKey, parameters.Length)) {
					ShellOut.Error("Command doesn't match the syntax. Please retype.");
					return false;
				}

				if (!command.HasParameters && parameters.Length > 0) {
					ShellOut.Info($"'{command.CommandName}' doesn't require any arguments.");

					string args = string.Empty;
					for (int i = 0; i < parameters.Length; i++) {
						if (!string.IsNullOrEmpty(parameters[i])) {
							args += parameters[i] + ",";
						}
					}

					ShellOut.Info($"Entered arguments '{args}' will be trimmed out.");
					parameters = new string[0];
				}

				if (parameters.Length > command.MaxParameterCount) {
					ShellOut.Info($"'{command.CommandName}' only supports a maximum of '{command.MaxParameterCount}' arguments. You have entered '{parameters.Length}'");

					string args = string.Empty;
					for (int i = (parameters.Length - command.MaxParameterCount) - 1; i > parameters.Length - command.MaxParameterCount; i--) {
						parameters[i] = null;
					}

					ShellOut.Info($"'{parameters.Length - command.MaxParameterCount}' arguments will be trimmed out.");
					return false;
				}

				Sound.PlayNotification(Sound.ENOTIFICATION_CONTEXT.ALERT);
				await command.ExecuteAsync(new Parameter(commandKey, parameters)).ConfigureAwait(false);
				return true;
			}
			finally {
				command.Dispose();
			}
		}

		/// <summary>
		/// Backwards compatibility for the old command.
		/// I always forget i dont need to type in ; anymore.
		/// Dang it.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		private static string Replace(string cmd) => cmd.Replace(";", "").Replace(",", "");
	}
}
