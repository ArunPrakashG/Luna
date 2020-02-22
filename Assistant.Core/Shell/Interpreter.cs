using Assistant.Core.Shell.Commands;
using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Parameter = Assistant.Extensions.Shared.Shell.Parameter;
/*
TODO: Implement Command loading based on IShellCommand Interface.
Load and run InitAsync() method
Assign loading assembly method for custom parsing and function to OnExecuteFunc()
Assign UniqueId to the command assembly

*/

namespace Assistant.Core.Shell {
	public static class Interpreter {
		internal static readonly Dictionary<string, IShellCommand> Commands = new Dictionary<string, IShellCommand>();
		internal static bool ShutdownShell = false;
		private const char LINE_SPLITTER = ';';
		private const string ExampleCommand = "relay -[param1],[param2];";
		private static readonly ILogger Logger = new Logger("INTERPRETER");
		public static int CommandsCount => Commands.Count;
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim LoopSync = new SemaphoreSlim(1, 1);
		internal static readonly Initializer Init = new Initializer();
		/// <summary>
		/// The current command which is being executed.
		/// </summary>
		public static string? CurrentCommand { get; private set; }
		private static bool InitCompleted = false;

		static Interpreter() {
			if (!Directory.Exists(Constants.COMMANDS_PATH)) {
				Directory.CreateDirectory(Constants.COMMANDS_PATH);
			}
		}

		public static async Task<bool> InitInterpreter<T>() where T : IShellCommand {
			if (InitCompleted) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				await LoadInternalCommands().ConfigureAwait(false);
				await Init.LoadCommandsAsync<T>().ConfigureAwait(false);

				InitCompleted = true;
				Helpers.InBackground(async () => await ShellLoop().ConfigureAwait(false));
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

		public static async Task ShellLoop() {
			if (!InitCompleted) {
				return;
			}

			await LoopSync.WaitAsync().ConfigureAwait(false);
			try {
				Console.WriteLine("Assistant Shell waiting for your commands!");
				do {
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write($"#~{Core.AssistantName} -> ");
					Console.ResetColor();
					string command = Console.ReadLine();
					//Console.Write("\n");

					if (string.IsNullOrEmpty(command) || string.IsNullOrWhiteSpace(command)) {
						ShellOut.Error("Please input valid command.");
						continue;
					}

					await ExecuteCommand(command).ConfigureAwait(false);
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

		private static async Task LoadInternalCommands() {
			IShellCommand helpCommand = new HelpCommand();
			await helpCommand.InitAsync().ConfigureAwait(false);
			lock (Commands) {
				Commands.Add(helpCommand.UniqueId, helpCommand);
			}

			IShellCommand bashCommand = new BashCommand();
			await bashCommand.InitAsync().ConfigureAwait(false);
			lock (Commands) {
				Commands.Add(bashCommand.UniqueId, bashCommand);
			}
		}

		private static async Task<bool> ExecuteCommand(string? command) {
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
			return await ParseCommand(command).ConfigureAwait(false);
		}

		private static async Task<bool> ParseCommand(string cmd) {
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

				//commands - returns {help -params}
				string[] split = cmd.Split(LINE_SPLITTER, StringSplitOptions.RemoveEmptyEntries);

				if (split == null || split.Length <= 0) {
					ShellOut.Error("Failed to parse the command. Please retype in correct syntax!");
					return false;
				}

				//for each command
				for (int i = 0; i < split.Length; i++) {
					if (string.IsNullOrEmpty(split[i])) {
						continue;
					}

					//splits the params - returns {help}{param1},{param2},{param3}...
					string[] split2 = split[i].Split('-', StringSplitOptions.RemoveEmptyEntries);

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
						return false;
					}

					if (!command.IsCurrentCommandContext(commandKey, parameters.Length)) {
						ShellOut.Error("Command doesn't match the syntax. Please retype.");
						return false;
					}

					if (!command.HasParameters && parameters.Length > 0) {
						ShellOut.Error("Command doesn't have any parameters and you have few arguments entered. What were you thinking ?");
						return false;
					}

					if (parameters.Length > command.MaxParameterCount) {
						ShellOut.Error("You have specified more than the allowed arguments for this command. Please use the backspace button.");
						return false;
					}

					await command.ExecuteAsync(new Parameter(commandKey, parameters)).ConfigureAwait(false);
					command.Dispose();
					return true;
				}

				ShellOut.Error("Command syntax is invalid. Re-execute the command with correct syntax.");
				return false;
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
	}
}
