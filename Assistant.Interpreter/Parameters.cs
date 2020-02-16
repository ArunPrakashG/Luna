using static Assistant.Interpreter.InterpreterCore;

namespace Assistant.Interpreter {
	internal class Parameters {
		public readonly string[] Values;
		public readonly COMMAND_CODE CommandCode;

		internal Parameters(string[] parameters, COMMAND_CODE code) {
			Values = parameters;
			CommandCode = code;
		}
	}
}
