using System;
using static Assistant.Interpreter.Interpreter;

namespace Assistant.Interpreter {
	public class CommandAction : ICommandFunction {
		public string? CommandName { get; set; }
		public Func<string[], (string? result, EXECUTE_RESULT code)> CommandFunctionObject { get; set; }
		public string? CommandDescription { get; set; }

		public CommandAction(Func<string[], (string? result, EXECUTE_RESULT code)> func, string? cmdName, string? cmdDescription) {
			CommandFunctionObject = func;
			CommandName = cmdName;
			CommandDescription = cmdDescription;
		}
	}
}
