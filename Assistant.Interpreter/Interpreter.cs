using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Interpreter.InterpreterCore;

namespace Assistant.Interpreter {
	public static class Interpreter {
		private static readonly Dictionary<string, Func<Parameters, (string? result, EXECUTE_RESULT code)>> InternalCommandFunctionPairs = new Dictionary<string, Func<Parameters, (string? result, EXECUTE_RESULT code)>>() {
			{"help", CommandProcessor.HelpCommand },
			{"relay", CommandProcessor.RelayCommand },
			{"device", CommandProcessor.DeviceCommand },
			{"gpio", CommandProcessor.GpioCommand },
			{"bash", CommandProcessor.BashCommand }
		};

		// <command_name, <command_param1, command_params[]>>
		public static readonly Dictionary<string, ICommandFunction> CommandFunctionPairs = new Dictionary<string, ICommandFunction>();

		private const char LINE_SPLITTER = ';';
		private const string ExampleCommand = "relay -[param1],[param2];";
		private static readonly ILogger Logger = new Logger("INTERPRETER");
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		public static string? CurrentCommand { get; private set; }
		private static bool InitCompleted = false;

		public static void InitInterpreter<T>(List<T> commandFunctions) where T : ICommandFunction {
			if (InitCompleted) {
				return;
			}

			if (commandFunctions.Count <= 0) {
				return;
			}

			for (int i = 0; i < commandFunctions.Count; i++) {
				string? cmdName = commandFunctions[i].CommandName;

				if (string.IsNullOrEmpty(cmdName) || commandFunctions[i].CommandFunctionObject == null) {
					continue;
				}

				CommandFunctionPairs.Add(cmdName, commandFunctions[i]);
			}

			InitCompleted = true;
		}

		internal static ICommandFunction? GetFunc(string? cmd) {
			if (string.IsNullOrEmpty(cmd) || CommandFunctionPairs.Count <= 0) {
				return null;
			}

			if (!CommandFunctionPairs.ContainsKey(cmd)) {
				return null;
			}

			if (!CommandFunctionPairs.TryGetValue(cmd, out ICommandFunction? func)) {
				return null;
			}

			return func;
		}

		public static async Task<(bool cmdStatus, string? cmdResponseObject)> ExecuteCommand(string? command) {
			if (!InitCompleted) {
				Logger.Warning("Interpreter isn't initiated properly.");
				return (false, "Interpreter is offline.");
			}

			if (string.IsNullOrEmpty(command)) {
				Logger.Trace("Command is null.");
				return (false, "Command empty or invalid.");
			}

			CurrentCommand = command;
			return await ParseCommand(command).ConfigureAwait(false);
		}

		private static async Task<(bool cmdStatus, string? cmdResponseObject)> ParseCommand(string? cmd) {
			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (!cmd.Contains(LINE_SPLITTER)) {
					return (false, $"Command syntax is invalid. maybe you are missing {LINE_SPLITTER} at the end ?");
				}
				string[] split = cmd.Split(LINE_SPLITTER);

				if (split == null || split.Length <= 0) {
					return (false, "Failed to parse the command. Please retype in correct syntax!");
				}

				for (int i = 0; i < split.Length; i++) {
					if (string.IsNullOrEmpty(split[i])) {
						continue;
					}

					string[] split2 = split[i].Split('-');

					if (split2 == null || split2.Length <= 0) {
						continue;
					}

					string? command = split2[0].Trim().ToLower();
					bool doesContainMultipleParams = !string.IsNullOrEmpty(split2[1]) && split2[1].Trim().Contains(',');
					string[] parameters = doesContainMultipleParams ?
						split2[1].Trim().Split(',')
						: new string[] { split2[1].Trim() };

					if (string.IsNullOrEmpty(command)) {
						continue;
					}

					if (InternalCommandFunctionPairs.ContainsKey(command)) {
						if (InternalCommandFunctionPairs.TryGetValue(command, out Func<Parameters, (string? result, EXECUTE_RESULT code)>? func)) {
							if (func == null) {
								continue;
							}

							Parameters values = new Parameters(parameters, GetCode(command, parameters, doesContainMultipleParams));

							(string? result, EXECUTE_RESULT code) = func.Invoke(values);

							if (!string.IsNullOrEmpty(result) && code == EXECUTE_RESULT.Success) {
								return (true, result);
							}

							return (false, !string.IsNullOrEmpty(result) ?
								result + " - " + code.ToString()
								: $"Execution failed. ({code.ToString()})");
						}
					}
					else {
						return (false, "Command doesn't exist. use 'help' to check all available commands!");
					}
				}

				return (false, "Command syntax is invalid. Re-execute the command with correct syntax.");
			}
			catch (Exception e) {
				Logger.Log(e);
				return (false, "Internal exception occurred. Execution failed forcefully.");
			}
			finally {
				Sync.Release();
			}
		}

		private static COMMAND_CODE GetCode(string? cmd, string[] values, bool multiParams) {
			if(string.IsNullOrEmpty(cmd) || values == null || values.Length <= 0) {
				return COMMAND_CODE.INVALID;
			}

			switch (cmd) {
				case "relay" when multiParams && values.Length == 2:
					return COMMAND_CODE.RELAY_BASIC;
				case "relay" when multiParams && values.Length == 3:
					return COMMAND_CODE.RELAY_DELAYED_TASK;
				case "help" when values.Length == 1:
					return COMMAND_CODE.HELP_ADVANCED;
				case "help" when values.Length == 0:
					return COMMAND_CODE.HELP_BASIC;
				case "help" when values.Length == 1 && !string.IsNullOrEmpty(values[0]) && values[0].Equals("all", StringComparison.OrdinalIgnoreCase):
					return COMMAND_CODE.HELP_ALL;
				case "bash" when values.Length == 1:
					return COMMAND_CODE.BASH_COMMAND;
				case "bash" when values.Length == 1 && !string.IsNullOrEmpty(values[0]) && File.Exists(values[0]):
					return COMMAND_CODE.BASH_SCRIPT_PATH;
				default:
					return COMMAND_CODE.INVALID;
			}
		}

		public enum EXECUTE_RESULT : byte {
			Success = 0x01,
			Failed = 0x00,
			InvalidArgs = 0x002,
			InvalidCommand = 0x003,
			DoesntExist = 0x004
		}
	}
}
