using Assistant.Core.Shell.Internal;
using Assistant.Extensions.Shared.Shell;
using System;
using static Assistant.Extensions.Shared.Shell.ShellEnum;

namespace Assistant.Core.Shell {
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
