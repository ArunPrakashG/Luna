namespace Assistant.Core.Shell {
	using Assistant.Core.Shell.InternalCommands;
	using Assistant.Extensions;
	using Assistant.Extensions.Shared.Shell;
	using Assistant.Logging;
	using Assistant.Logging.Interfaces;
	using Assistant.Sound;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using Parameter = Extensions.Shared.Shell.Parameter;

	/// <summary>
	/// The Shell Instance.
	/// </summary>
	public static class Interpreter {
		/// <summary>
		/// Defines the Logger
		/// </summary>
		private static readonly ILogger Logger = new Logger("INTERPRETER");

		/// <summary>
		/// Defines the Sync
		/// </summary>
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Defines the LoopSync
		/// </summary>
		private static readonly SemaphoreSlim LoopSync = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Defines the LINE_SPLITTER
		/// </summary>
		private const char LINE_SPLITTER = ';';

		/// <summary>
		/// Defines the InitCompleted
		/// </summary>
		private static bool InitCompleted = false;

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
		internal static bool ShutdownShell = false;

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
		/// <typeparam name="T">The Shell command type. <b>(IShellCommand)</b></typeparam>
		/// <returns>Boolean, indicating if the startup was successful.</returns>
		public static async Task<bool> InitInterpreterAsync<T>() where T : IShellCommand {
			if (InitCompleted) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				await Init.LoadInternalCommandsAsync<T>().ConfigureAwait(false);
				//await LoadInternalCommandsAsync().ConfigureAwait(false);
				await Init.LoadCommandsAsync<T>().ConfigureAwait(false);

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

		/// <summary>
		/// The ReplAsync
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		private static async Task ReplAsync() {
			if (!InitCompleted) {
				return;
			}

			await LoopSync.WaitAsync().ConfigureAwait(false);
			try {
				Console.WriteLine("Assistant Shell waiting for your commands!");
				do {
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write($"#~/{Core.AssistantName.Trim()}/$ ]> ");
					Console.ResetColor();
					string command = Console.ReadLine();

					if (string.IsNullOrEmpty(command) || string.IsNullOrWhiteSpace(command)) {
						ShellOut.Error("Please input a valid command.");
						continue;
					}

					await ExecuteCommandAsync(command).ConfigureAwait(false);
				} while (!ShutdownShell);
			}
			catch (Exception e) {
				Logger.Exception(e);
				return;
			}
			finally {
				LoopSync.Release();
				Logger.Info("Shell has been shutdown.");
			}
		}

		/// <summary>
		/// The LoadInternalCommandsAsync
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		private static async Task LoadInternalCommandsAsync() {
			IShellCommand helpCommand = new HelpCommand();
			if (!helpCommand.IsInitSuccess) {
				await helpCommand.InitAsync().ConfigureAwait(false);
			}

			lock (Commands) {
				Commands.Add(helpCommand.UniqueId, helpCommand);
			}

			IShellCommand exitCommand = new ExitCommand();
			if (!exitCommand.IsInitSuccess) {
				await exitCommand.InitAsync().ConfigureAwait(false);
			}

			lock (Commands) {
				Commands.Add(exitCommand.UniqueId, exitCommand);
			}

			IShellCommand gpioCommand = new GpioCommand();
			if (!gpioCommand.IsInitSuccess) {
				await gpioCommand.InitAsync().ConfigureAwait(false);
			}

			lock (Commands) {
				Commands.Add(gpioCommand.UniqueId, gpioCommand);
			}
		}

		/// <summary>
		/// The ExecuteCommandAsync
		/// </summary>
		/// <param name="command">The command<see cref="string?"/></param>
		/// <returns>The <see cref="Task{bool}"/></returns>
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
		/// The ParseCommandAsync
		/// </summary>
		/// <param name="cmd">The cmd<see cref="string"/></param>
		/// <returns>The <see cref="Task{bool}"/></returns>
		private static async Task<bool> ParseCommandAsync(string cmd) {
			if (Commands.Count <= 0) {
				ShellOut.Info("No commands have been loaded into the shell.");
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (!cmd.Contains(LINE_SPLITTER)) {
					ShellOut.Error($"Command syntax is invalid. maybe you are missing '{LINE_SPLITTER}' at the end ?");
					return false;
				}

				//commands - returns {help -argument}
				string[] split = cmd.Split(LINE_SPLITTER, StringSplitOptions.RemoveEmptyEntries);

				if (split == null || split.Length <= 0) {
					ShellOut.Error("Failed to parse the command. Please retype in correct syntax!");
					return false;
				}

				bool anyExec = false;
				//for each command
				for (int i = 0; i < split.Length; i++) {
					if (string.IsNullOrEmpty(split[i])) {
						continue;
					}

					//splits the arguments - returns {help}{param1},{param2},{param3}...
					string[] split2 = split[i].Split('-', StringSplitOptions.RemoveEmptyEntries);

					foreach (string val in split2) {
						if (string.IsNullOrEmpty(val)) {
							continue;
						}

						val.Trim();
					}

					if (split2 == null || split2.Length <= 0) {
						continue;
					}

					string? commandKey = split2[0].Trim().ToLower();
					bool doesContainParams = split2.Length > 1 && !string.IsNullOrEmpty(split2[1]);
					bool doesContainMultipleParams = doesContainParams && split2[1].Trim().Contains(',');
					string[] parameters = doesContainMultipleParams ?
						split2[1].Trim().Split(',')
						: doesContainParams ?
						new string[] { split2[1].Trim() }
						: new string[] { };

					if (string.IsNullOrEmpty(commandKey)) {
						continue;
					}

					IShellCommand command = await Init.GetCommandWithKeyAsync<IShellCommand>(commandKey).ConfigureAwait(false);

					if (command == null) {
						ShellOut.Error("Command doesn't exist. use 'help' to check all available commands!");
						continue;
					}

					if (!command.IsInitSuccess) {
						await command.InitAsync().ConfigureAwait(false);
					}

					if (!command.IsCurrentCommandContext(commandKey, parameters.Length)) {
						ShellOut.Error("Command doesn't match the syntax. Please retype.");
						continue;
					}

					if (!command.HasParameters && parameters.Length > 0) {
						ShellOut.Error("Command doesn't have any parameters and you have few arguments entered. What were you thinking ?");
						continue;
					}

					if (parameters.Length > command.MaxParameterCount) {
						ShellOut.Error("You have specified more than the allowed arguments for this command. Please use the backspace button.");
						continue;
					}

					await command.ExecuteAsync(new Parameter(commandKey, parameters)).ConfigureAwait(false);
					command.Dispose();
					Sound.PlayNotification(Sound.ENOTIFICATION_CONTEXT.ALERT);
					anyExec = true;
					continue;
				}

				if (!anyExec) {
					ShellOut.Error("Command syntax is invalid. Re-execute the command with correct syntax.");
					return false;
				}

				return true;
			}
			catch (Exception e) {
				Logger.Log(e);
				ShellOut.Error("Internal exception occurred. Execution failed forcefully.");
				ShellOut.Exception(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}

		/// <summary>
		/// Defines the EXECUTE_RESULT
		/// </summary>
		public enum EXECUTE_RESULT : byte {
			/// <summary>
			/// Defines the Success
			/// </summary>
			Success = 0x01,

			/// <summary>
			/// Defines the Failed
			/// </summary>
			Failed = 0x00,

			/// <summary>
			/// Defines the InvalidArgs
			/// </summary>
			InvalidArgs = 0x002,

			/// <summary>
			/// Defines the InvalidCommand
			/// </summary>
			InvalidCommand = 0x003,

			/// <summary>
			/// Defines the DoesntExist
			/// </summary>
			DoesntExist = 0x004
		}
	}
}
