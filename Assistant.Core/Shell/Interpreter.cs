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
				LoadInternalCommands<T>();
				await Init.LoadCommandsAsync<T>().ConfigureAwait(false);

				if (Initializer.Commands.Count > 0) {
					foreach (T cmd in Initializer.Commands) {
						lock (Commands) {
							Commands.Add(cmd.UniqueId, cmd);
						}
					}
				}

				InitCompleted = true;
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

			Console.WriteLine("Assistant Shell waiting for your commands!");
			do {				
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write($"#~{Core.AssistantName} -> ");
				Console.ResetColor();
				string command = Console.ReadLine();

				if (string.IsNullOrEmpty(command) || string.IsNullOrWhiteSpace(command)) {
					ShellOut.Error("Please input valid command.");
					continue;
				}

				await ExecuteCommand(command).ConfigureAwait(false);
			} while (!ShutdownShell);
		}

		private static void LoadInternalCommands<T>() where T : IShellCommand {
			IShellCommand helpCommand = new HelpCommand();
			lock (Commands) {
				Commands.Add(helpCommand.UniqueId, helpCommand);
			}

			IShellCommand bashCommand = new BashCommand();
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
					ShellOut.Error($"Command syntax is invalid. maybe you are missing {LINE_SPLITTER} at the end ?");
					return false;
				}
				string[] split = cmd.Split(LINE_SPLITTER);

				if (split == null || split.Length <= 0) {
					ShellOut.Error("Failed to parse the command. Please retype in correct syntax!");
					return false;
				}

				for (int i = 0; i < split.Length; i++) {
					if (string.IsNullOrEmpty(split[i])) {
						continue;
					}

					string[] split2 = split[i].Split('-');

					if (split2 == null || split2.Length <= 0) {
						continue;
					}

					string? commandKey = split2[0].Trim().ToLower();
					bool doesContainMultipleParams = !string.IsNullOrEmpty(split2[1]) && split2[1].Trim().Contains(',');
					string[] parameters = doesContainMultipleParams ?
						split2[1].Trim().Split(',')
						: new string[] { split2[1].Trim() };

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
