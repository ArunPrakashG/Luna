using static Assistant.Shell.InterpreterCore;

namespace Assistant.Shell.Internal {
	internal class Parameter {
		public readonly string[] Parameters;
		public readonly COMMAND_CODE CommandCode;

		internal Parameter(string[] parameters, COMMAND_CODE code) {
			Parameters = parameters;
			CommandCode = code;
		}
	}
}
