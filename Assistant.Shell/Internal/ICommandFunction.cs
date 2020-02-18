using System;
using static Assistant.Shell.InterpreterCore;

namespace Assistant.Shell.Internal {
	public interface ICommandFunction {
		string? CommandName { get; }
		string? CommandDescription { get; }
		COMMAND_CODE CommandCode { get; }
		Func<Parameter, Response?> CommandFunctionObject { get; }
	}
}
