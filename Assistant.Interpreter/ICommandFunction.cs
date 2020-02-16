using System;
using static Assistant.Interpreter.Interpreter;
using static Assistant.Interpreter.InterpreterCore;

namespace Assistant.Interpreter {
	public interface ICommandFunction {
		string? CommandName { get; set; }
		string? CommandDescription { get; set; }
		COMMAND_CODE CommandCode { get; set; }
		Func<string[], (string? result, EXECUTE_RESULT code)> CommandFunctionObject { get; set; }
	}
}
