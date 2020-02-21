using System;
using System.Threading.Tasks;
using static Assistant.Extensions.Shared.Shell.ShellEnum;

namespace Assistant.Extensions.Shared.Shell {
	public interface IShellCommand {
		bool HasParameters { get; }
		string CommandName { get; }
		int MaxParameterCount { get; }
		string UniqueId => (CommandName.GetTypeCode().GetHashCode() + CommandName.GetHashCode() + MaxParameterCount).ToString()
							+ "_" + CommandDescription.GetHashCode().ToString();
		string CommandDescription { get; }
		string CommandKey { get; }
		COMMAND_CODE CommandCode { get; }

		bool IsCurrentCommandContext(string command, int paramsCount) {
			if (string.IsNullOrEmpty(command) || paramsCount < 0) {
				return false;
			}

			if (!command.Equals(CommandKey, StringComparison.OrdinalIgnoreCase)) {
				return false;
			}

			if (HasParameters && MaxParameterCount == paramsCount) {
				return true;
			}

			return false;
		}

		bool Parse(Parameter parameter);
		Task ExecuteAsync(Parameter parameter);
		Task InitAsync();
		Func<Parameter, bool> OnExecuteFunc { get; set; }
		void Dispose();
	}
}
