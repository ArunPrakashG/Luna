using Assistant.Core.Shell.Internal;
using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Core.Shell.InterpreterCore;
using static Assistant.Extensions.Shared.Shell.ShellEnum;
/*
	TODO: Implement Command loading based on IShellCommand Interface.
		  Load and run InitAsync() method
		  Assign loading assembly method for custom parsing and function to OnExecuteFunc()
		  Assign UniqueId to the command assembly

*/

namespace Assistant.Core.Shell {
	public static class Interpreter {
		private static readonly Dictionary<string, Func<Parameter, Response?>> InternalCommandFunctionPairs = new Dictionary<string, Func<Parameter, Response?>>() {
			{"help", Processor.HelpCommand },
			{"relay", Processor.RelayCommand },
			{"device", Processor.DeviceCommand },
			{"gpio", Processor.GpioCommand },
			{"bash", Processor.BashCommand }
		};

		// <command_code, ICommandFunction>
		public static readonly Dictionary<COMMAND_CODE, ICommandFunction> CommandFunctionPairs = new Dictionary<COMMAND_CODE, ICommandFunction>();
		internal static readonly List<ICommandFunction> CommandsCollection = new List<ICommandFunction>();
		private const char LINE_SPLITTER = ';';
		private const string ExampleCommand = "relay -[param1],[param2];";
		private static readonly ILogger Logger = new Logger("INTERPRETER");
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		public static string? CurrentCommand { get; private set; }
		private static bool InitCompleted = false;

		static Interpreter() {
			if (!Directory.Exists(Constants.COMMANDS_PATH)) {
				Directory.CreateDirectory(Constants.COMMANDS_PATH);
			}
		}

		public static void InitInterpreter<T>(List<T> commandFunctions) where T : ICommandFunction {
			if (InitCompleted) {
				return;
			}

			if (commandFunctions.Count <= 0) {
				return;
			}

			for (int i = 0; i < commandFunctions.Count; i++) {
				if (commandFunctions[i].CommandFunctionObject == null) {
					continue;
				}

				CommandsCollection.Add(commandFunctions[i]);
				CommandFunctionPairs.Add(commandFunctions[i].CommandCode, commandFunctions[i]);
			}

			if (InternalCommandFunctionPairs.Count > 0) {
				foreach (KeyValuePair<string, Func<Parameter, Response?>> cmd in InternalCommandFunctionPairs) {
					if (string.IsNullOrEmpty(cmd.Key)) {
						continue;
					}

					ICommandFunction? cmdFunc = default;
					switch (cmd.Key) {
						case "relay":
							cmdFunc = new CommandFunction(cmd.Value, cmd.Key, COMMAND_CODE.RELAY_BASIC, "Command to control relay board connected with pi.");
							break;
						case "help":
							cmdFunc = new CommandFunction(cmd.Value, cmd.Key, COMMAND_CODE.HELP_BASIC, "Displays all the commands and their description.");
							break;
						case "gpio":
							cmdFunc = new CommandFunction(cmd.Value, cmd.Key, COMMAND_CODE.GPIO_CYCLE_TEST, "Command to control Gpio pins on the raspberry pi board.");
							break;
						case "device":
							cmdFunc = new CommandFunction(cmd.Value, cmd.Key, COMMAND_CODE.DEVICE_REBOOT, "Command for device related functions.");
							break;
						case "bash":
							cmdFunc = new CommandFunction(cmd.Value, cmd.Key, COMMAND_CODE.HELP_BASIC, "Command provides functionality to execute bash script/command.");
							break;
					}

					if (cmdFunc != null)
						CommandsCollection.Add(cmdFunc);
				}
			}

			InitCompleted = true;
		}

		internal static ICommandFunction? GetFunc(COMMAND_CODE code) {
			if (code == COMMAND_CODE.INVALID || CommandFunctionPairs.Count <= 0) {
				return null;
			}

			if (!CommandFunctionPairs.ContainsKey(code)) {
				return null;
			}

			if (!CommandFunctionPairs.TryGetValue(code, out ICommandFunction? func)) {
				return null;
			}

			return func;
		}

		public static async Task<ParseResponse> ExecuteCommand(string? command) {
			if (!InitCompleted) {
				Logger.Warning("Interpreter isn't initiated properly.");
				return new ParseResponse(false, "Interpreter is offline.");
			}

			if (string.IsNullOrEmpty(command)) {
				Logger.Trace("Command is null.");
				return new ParseResponse(false, "Command empty or invalid.");
			}

			CurrentCommand = command;
			return await ParseCommand(command).ConfigureAwait(false);
		}

		private static async Task<ParseResponse> ParseCommand(string cmd) {
			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (!cmd.Contains(LINE_SPLITTER)) {
					return new ParseResponse(false, $"Command syntax is invalid. maybe you are missing {LINE_SPLITTER} at the end ?");
				}
				string[] split = cmd.Split(LINE_SPLITTER);

				if (split == null || split.Length <= 0) {
					return new ParseResponse(false, "Failed to parse the command. Please retype in correct syntax!");
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
						if (InternalCommandFunctionPairs.TryGetValue(command, out Func<Parameter, Response?>? func)) {
							if (func == null) {
								continue;
							}

							Parameter values = new Parameter(parameters, GetCode(command, parameters, doesContainMultipleParams));

							Response? response = func.Invoke(values);

							if (response != null && response.HasValue && response.Value.ResultCode == EXECUTE_RESULT.Success && !string.IsNullOrEmpty(response.Value.ExecutionResult)) {
								return new ParseResponse(true, response.Value.ExecutionResult);
							}

							return new ParseResponse(false, response != null && !string.IsNullOrEmpty(response.Value.ExecutionResult) ?
								response.Value.ExecutionResult + " - " + response.Value.ResultCode.ToString()
								: $"Execution failed. ({response.Value.ResultCode.ToString()})");
						}
					}
					else {
						return new ParseResponse(false, "Command doesn't exist. use 'help' to check all available commands!");
					}
				}

				return new ParseResponse(false, "Command syntax is invalid. Re-execute the command with correct syntax.");
			}
			catch (Exception e) {
				Logger.Log(e);
				return new ParseResponse(false, "Internal exception occurred. Execution failed forcefully.");
			}
			finally {
				Sync.Release();
			}
		}

		private static COMMAND_CODE GetCode(string? cmd, string[] values, bool multiParams) {
			if (string.IsNullOrEmpty(cmd) || values == null || values.Length <= 0) {
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
	}
}
