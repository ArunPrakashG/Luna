using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Interpreter {
	internal static class CommandProcessor {
		internal static (bool cmdStatus, T cmdResponseObject) OnRelayCommand<T>(bool isOn, int pinNumber, int delay) where T : class {
			if (pinNumber <= 0) {
				return default;
			}

			
		}

		internal static void HelpCommand(string[] parameters) {

		}

		internal static void GpioCommand(string[] parameters) {

		}

		internal static void DeviceCommand(string[] parameters) {

		}

		internal static void RelayCommand(string[] parameters) {
			if (string.IsNullOrEmpty(parameters[0]) || string.IsNullOrEmpty(parameters[1])) {
				return;
			}

			switch (parameters.Length) {
				//generic relay command, on/off
				case 2:
					if (!bool.TryParse(parameters[0], out bool isOn)) {
						return;
					}

					if (!int.TryParse(parameters[1], out int pinNumber)) {
						return;
					}

					//TODO: Create array of actions to be executed for each pin number.
					//Execute the specified event for pin number
					break;
				//relay delay command, on/off with delay value (mins)
				case 3:
					break;
			}
			


		}
	}
}
