using System;
using static Assistant.Interpreter.Interpreter;

namespace Assistant.Interpreter {
	public interface ICommandFunction {
		string? CommandName { get; set; }
		string? CommandDescription { get; set; }
		Func<string[], (string? result, EXECUTE_RESULT code)> CommandFunctionObject { get; set; }
	}
}
