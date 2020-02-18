using System;
using Assistant.Shell.Internal;
using static Assistant.Shell.InterpreterCore;

namespace Assistant.Shell {	
	public class CommandFunction : ICommandFunction {
		public string? CommandName { get; }
		public Func<Parameter, Response?> CommandFunctionObject { get; }
		public string? CommandDescription { get; }
		public COMMAND_CODE CommandCode { get; }

		public CommandFunction(Func<Parameter, Response?> func, string? cmdName, COMMAND_CODE code, string? cmdDescription) {
			CommandFunctionObject = func;
			CommandName = cmdName;
			CommandCode = code;
			CommandDescription = cmdDescription;
		}
	}
}
