using Assistant.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Assistant.Interpreter.Interpreter;

namespace Assistant.Interpreter {
	internal static class CommandProcessor {

		internal static (string? result, EXECUTE_RESULT code) HelpCommand(string[] parameters) {

		}

		internal static (string? result, EXECUTE_RESULT code) GpioCommand(string[] parameters) {

		}

		internal static (string? result, EXECUTE_RESULT code) DeviceCommand(string[] parameters) {

		}

		internal static (string? result, EXECUTE_RESULT code) RelayCommand(string[] parameters) {
			if (string.IsNullOrEmpty(parameters[0]) || string.IsNullOrEmpty(parameters[1])) {
				return (null, EXECUTE_RESULT.InvalidArgs);
			}

			(string? result, EXECUTE_RESULT code)? response;
			bool isOn;
			int pinNumber;

			switch (parameters.Length) {
				//generic relay command, on/off
				case 2:
					if (!bool.TryParse(parameters[0], out isOn)) {
						return ("Invalid Argument: Pin state could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(parameters[1], out pinNumber)) {
						return ("Invalid Argument: Pin number could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}
					
					response = GetFunc(parameters[1])?.Invoke(parameters);

					if(response == null || !response.HasValue) {
						return ("Command execution failed.", EXECUTE_RESULT.Failed);
					}

					return response.Value;
				//relay delay command, on/off with delay value (mins)
				case 3:
					if (!bool.TryParse(parameters[0], out isOn)) {
						return ("Invalid Argument: Pin state could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(parameters[1], out pinNumber)) {
						return ("Invalid Argument: Pin number could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					 response = GetFunc(parameters[1])?.Invoke(parameters);

					if (response == null || !response.HasValue) {						
						return ("Command execution failed.", EXECUTE_RESULT.Failed);
					}

					return response.Value;
				default:
					return ("Command parameters are invalid.", EXECUTE_RESULT.InvalidCommand);
			}
		}
	}
}
