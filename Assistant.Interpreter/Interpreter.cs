using Assistant.Interpreter.Events;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Interpreter {
	public static class Interpreter {
		private static Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>() {
			{"help", CommandProcessor.HelpCommand },
			{"relay", CommandProcessor.RelayCommand },
			{"device", CommandProcessor.DeviceCommand },
			{"gpio", CommandProcessor.GpioCommand }
		};
		private const char LINE_SPLITTER = ';';
		private const string ExampleCommand = "relay -[param1],[param2];";
		private static readonly ILogger Logger = new Logger("INTERPRETER");
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static string? CurrentCommand;

		public delegate void OnRelayCommandInterpreted(object sender, OnRelayCommandEventArgs e);
		public static event OnRelayCommandInterpreted? OnRelayCommand;
		public delegate void OnGpioCommandInterpreted(object sender, OnGpioCommandEventArgs e);
		public static event OnGpioCommandInterpreted? OnGpioCommand;
		public delegate void OnDeviceCommandInterpreted(object sender, OnDeviceCommandEventArgs e);
		public static event OnDeviceCommandInterpreted? OnDeviceCommand;

		public static async Task<(bool cmdStatus, T cmdResponseObject)> ExecuteCommand<T>(string? command) where T : class {
			if (string.IsNullOrEmpty(command)) {
				Logger.Trace("Command is null.");
				return default;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {

			}
			catch (Exception e) {

			}
			finally {
				Sync.Release();
			}
		}

		private static (bool cmdStatus, T cmdResponseObject) ParseCommand<T>(string? cmd) where T : class {
			if (string.IsNullOrEmpty(cmd)) {
				return default;
			}

			try {
				string[] split = cmd.Split(LINE_SPLITTER);

				if (split == null || split.Length <= 0) {
					return default;
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

					if (Commands.ContainsKey(command)) {
						if(Commands.TryGetValue(command, out Action<string[]>? func)) {
							if(func == null) {
								continue;
							}

							func.Invoke(parameters);
						}
					}

					//bool isOn;
					//int pinNumber;
					//int delayInMins;

					////TODO: Add rest of the  commands
					//switch (command) {
					//	case "RELAY" when parameters != null && parameters.Length == 2:
					//		if (string.IsNullOrEmpty(parameters[0]) || string.IsNullOrEmpty(parameters[1])) {
					//			continue;
					//		}

					//		if (!bool.TryParse(parameters[0], out isOn)) {
					//			continue;
					//		}

					//		if (!int.TryParse(parameters[1], out pinNumber)) {
					//			continue;
					//		}

					//		OnRelayCommand?.Invoke(null, new OnRelayCommandEventArgs(pinNumber, isOn, 0));
					//		return CommandProcessor.OnRelayCommand<T>(isOn, pinNumber, 0);
					//	case "RELAY" when parameters != null && parameters.Length == 3:
					//		if (string.IsNullOrEmpty(parameters[0]) || string.IsNullOrEmpty(parameters[1]) || string.IsNullOrEmpty(parameters[2])) {
					//			continue;
					//		}

					//		if (!bool.TryParse(parameters[0], out isOn)) {
					//			continue;
					//		}

					//		if (!int.TryParse(parameters[1], out pinNumber)) {
					//			continue;
					//		}

					//		if (!int.TryParse(parameters[2], out delayInMins)) {
					//			continue;
					//		}

					//		OnRelayCommand?.Invoke(null, new OnRelayCommandEventArgs(pinNumber, isOn, 0));
					//		return CommandProcessor.OnRelayCommand<T>(isOn, pinNumber, delayInMins);
					//}
				}
			}
			catch (Exception e) {

			}
			finally {

			}
		}
	}
}
