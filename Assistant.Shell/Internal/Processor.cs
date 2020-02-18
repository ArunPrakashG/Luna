using static Assistant.Shell.Interpreter;
using static Assistant.Shell.InterpreterCore;

namespace Assistant.Shell.Internal {
	internal static class Processor {

		/// <summary>
		/// Internal method to handle Help commands.
		/// </summary>
		/// <param name="p">Parameter object consisting of string[] of parameters and command code.</param>
		/// <returns>Tuple, string result and Execution code which states Success/Failure/Error status.</returns>
		internal static Response? HelpCommand(Parameter p) {

		}

		internal static Response? GpioCommand(Parameter p) {

		}

		internal static Response? DeviceCommand(Parameter p) {

		}

		/// <summary>
		/// Internal method to handle all relay commands.
		/// </summary>
		/// <param name="p">Parameter object consisting of string[] of parameters and command code.</param>
		/// <returns>Tuple, string result and Execution code which states Success/Failure/Error status.</returns>
		internal static Response? RelayCommand(Parameter p) {
			if (p.CommandCode == COMMAND_CODE.INVALID || p.Parameters == null || p.Parameters.Length <= 0) {
				return null;
			}

			Response? response;
			bool? isOn;
			int pinNumber;

			switch (p.CommandCode) {
				//generic relay command, on/off
				case COMMAND_CODE.RELAY_BASIC:
					if (!p.Parameters[0].AsBool(out isOn)) {
						return new Response("Invalid Argument: Pin state could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(p.Parameters[1], out pinNumber)) {
						return new Response("Invalid Argument: Pin number could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					response = GetFunc(p.CommandCode)?.CommandFunctionObject.Invoke(p);

					if (response == null || string.IsNullOrEmpty(response.Value.ExecutionResult)) {
						return new Response("Command execution failed.", EXECUTE_RESULT.Failed);
					}

					return response;
				//relay delay command, on/off with delay value (mins)
				case COMMAND_CODE.RELAY_DELAYED_TASK:
					if (!p.Parameters[0].AsBool(out isOn)) {
						return new Response("Invalid Argument: Pin state could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(p.Parameters[1], out pinNumber)) {
						return new Response("Invalid Argument: Pin number could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					if (!int.TryParse(p.Parameters[2], out int delay)) {
						return new Response("Invalid Argument: Delay could not be parsed.", EXECUTE_RESULT.InvalidArgs);
					}

					response = GetFunc(p.CommandCode)?.CommandFunctionObject.Invoke(p);

					if (response == null || string.IsNullOrEmpty(response.Value.ExecutionResult)) {
						return new Response("Command execution failed.", EXECUTE_RESULT.Failed);
					}

					return response;
				default:
					return new Response("Command parameters are invalid.", EXECUTE_RESULT.InvalidCommand);
			}
		}

		/// <summary>
		/// Internal method to handle Bash command.
		/// </summary>
		/// <param name="p">Parameter object consisting of string[] of parameters and command code.</param>
		/// <returns>Tuple, string result and Execution code which states Success/Failure/Error status.</returns>
		internal static Response? BashCommand(Parameter p) {

		}
	}
}
