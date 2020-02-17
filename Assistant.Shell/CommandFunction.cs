using System;
using Assistant.Shell.Internal;
using static Assistant.Shell.InterpreterCore;

namespace Assistant.Shell {	
	public class CommandFunction : ICommandFunction {
		public string? CommandName { get; set; }
		public Func<string[], (string? result, EXECUTE_RESULT code)> CommandFunctionObject { get; set; }
		public string? CommandDescription { get; set; }
		public COMMAND_CODE CommandCode { get; set; }

		public CommandFunction(Func<string[], (string? result, EXECUTE_RESULT code)> func, string? cmdName, COMMAND_CODE code, string? cmdDescription) {
			CommandFunctionObject = func;
			CommandName = cmdName;
			CommandCode = code;
			CommandDescription = cmdDescription;
		}
	}
}
