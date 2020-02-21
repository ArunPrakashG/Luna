using System;
using static Assistant.Extensions.Shared.Shell.ShellEnum;

namespace Assistant.Core.Shell.Internal {
	public interface ICommandFunction {
		string? CommandName { get; }
		string? CommandDescription { get; }
		COMMAND_CODE CommandCode { get; }
		Func<Parameter, Response?> CommandFunctionObject { get; }
	}
}
