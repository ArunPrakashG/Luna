using Assistant.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Assistant.Interpreter.Interpreter;
using static Assistant.Interpreter.InterpreterCore;

namespace Assistant.Interpreter {
	internal static class CommandProcessor {

		internal static (string? result, EXECUTE_RESULT code) HelpCommand(Parameters p) {

		}

		internal static (string? result, EXECUTE_RESULT code) GpioCommand(Parameters p) {

		}

		internal static (string? result, EXECUTE_RESULT code) DeviceCommand(Parameters p) {

		}

		internal static (string? result, EXECUTE_RESULT code) RelayCommand(Parameters p) {
			if (p == null || p.CommandCode == COMMAND_CODE.INVALID || p.Values == null || p.Values.Length <= 0) {
				return (null, EXECUTE_RESULT.InvalidCommand);
			}

			(string? result, EXECUTE_RESULT code)? response;
			bool? isOn;
			int pinNumber;

			switch (p.CommandCode) {
				//generic relay command, on/off
				case COMMAND_CODE.RELAY_BASIC:
					if (!p.Values[0].AsBool(out isOn)) {
						return ("Invalid Argument: Pin state could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(p.Values[1], out pinNumber)) {
						return ("Invalid Argument: Pin number could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					response = GetFunc(p.Values[1])?.CommandFunctionObject.Invoke(p.Values);

					if (response == null || !response.HasValue) {
						return ("Command execution failed.", EXECUTE_RESULT.Failed);
					}

					return response.Value;
				//relay delay command, on/off with delay value (mins)
				case COMMAND_CODE.RELAY_DELAYED_TASK:
					if (!p.Values[0].AsBool(out isOn)) {
						return ("Invalid Argument: Pin state could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(p.Values[1], out pinNumber)) {
						return ("Invalid Argument: Pin number could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(p.Values[2], out int delay)) {
						return ("Invalid Argument: Delay could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					response = GetFunc(p.Values[1])?.CommandFunctionObject.Invoke(p.Values);

					if (response == null || !response.HasValue) {						
						return ("Command execution failed.", EXECUTE_RESULT.Failed);
					}

					return response.Value;
				default:
					return ("Command parameters are invalid.", EXECUTE_RESULT.InvalidCommand);
			}
		}

		internal static (string? result, EXECUTE_RESULT code) BashCommand(Parameters p) {
			
		}
	}
}
