using Assistant.Interpreter.Events;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Interpreter {
	public static class Interpreter {
		private static readonly Dictionary<string, Func<string[], (string? result, EXECUTE_RESULT code)>> InternalCommandActions = new Dictionary<string, Func<string[], (string? result, EXECUTE_RESULT code)>>() {
			{"help", CommandProcessor.HelpCommand },
			{"relay", CommandProcessor.RelayCommand },
			{"device", CommandProcessor.DeviceCommand },
			{"gpio", CommandProcessor.GpioCommand }
		};

		// <command_name, <command_param1, command_params[]>>
		public static Dictionary<string, Func<string[], (string? result, EXECUTE_RESULT code)>> CommandActionPairs { get; private set; } = new Dictionary<string, Func<string[], (string? result, EXECUTE_RESULT code)>>();

		private const char LINE_SPLITTER = ';';
		private const string ExampleCommand = "relay -[param1],[param2];";
		private static readonly ILogger Logger = new Logger("INTERPRETER");
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		public static string? CurrentCommand { get; private set; }
		private static bool AlreadyInitCompleted = false;

		public delegate void OnRelayCommandInterpreted(object sender, OnRelayCommandEventArgs e);
		public static event OnRelayCommandInterpreted? OnRelayCommand;
		public delegate void OnGpioCommandInterpreted(object sender, OnGpioCommandEventArgs e);
		public static event OnGpioCommandInterpreted? OnGpioCommand;
		public delegate void OnDeviceCommandInterpreted(object sender, OnDeviceCommandEventArgs e);
		public static event OnDeviceCommandInterpreted? OnDeviceCommand;

		public static void InitInterpreter<T>(List<T> commandActions) where T : ICommandFunction {
			if (AlreadyInitCompleted) {
				return;
			}

			AlreadyInitCompleted = true;

			if(commandActions.Count <= 0) {
				return;
			}

			for(int i = 0; i < commandActions.Count; i++) {
				if(string.IsNullOrEmpty(commandActions[i].CommandName) || commandActions[i].CommandFunctionObject == null) {
					continue;
				}

				CommandActionPairs.Add(commandActions[i].CommandName, commandActions[i].CommandFunctionObject);
			}
		}

		internal static Func<string[], (string? result, EXECUTE_RESULT code)>? GetFunc(string? cmd) {
			if (string.IsNullOrEmpty(cmd) || CommandActionPairs.Count <= 0) {
				return null;
			}

			if (!CommandActionPairs.ContainsKey(cmd)) {
				return null;
			}

			CommandActionPairs.TryGetValue(cmd, out Func<string[], (string? result, EXECUTE_RESULT code)>? func);
			return func;
		}

		public static async Task<(bool cmdStatus, string? cmdResponseObject)> ExecuteCommand(string? command) {
			if (!AlreadyInitCompleted) {
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
					string[] parameters = doesContainMultipleParams ? split2[1].Trim().Split(',') : new string[] { split2[1].Trim() };

					if (string.IsNullOrEmpty(command)) {
						continue;
					}

					if (InternalCommandActions.ContainsKey(command)) {
						if(InternalCommandActions.TryGetValue(command, out Func<string[], (string? result, EXECUTE_RESULT code)>? func)) {
							if(func == null) {
								continue;
							}

							(string? result, EXECUTE_RESULT code) = func.Invoke(parameters);

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

		public enum EXECUTE_RESULT : byte {
			Success = 0x01,
			Failed = 0x00,
			InvalidArgs = 0x002,
			InvalidCommand = 0x003,
			DoesntExist = 0x004
		}
	}
}
