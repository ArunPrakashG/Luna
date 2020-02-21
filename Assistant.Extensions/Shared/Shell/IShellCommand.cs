using System;
using System.Threading.Tasks;
using static Assistant.Extensions.Shared.Shell.ShellEnum;

namespace Assistant.Extensions.Shared.Shell {
	/// <summary>
	/// Base implementation of a shell command object. Must be inherited by all ShellCommand objects.
	/// </summary>
	public interface IShellCommand {
		/// <summary>
		/// Indicates if the command has any parameters.
		/// </summary>
		bool HasParameters { get; }
		/// <summary>
		/// The Command Name
		/// </summary>
		string CommandName { get; }
		/// <summary>
		/// Maximum amount of parameter values this command supports globally.
		/// </summary>
		int MaxParameterCount { get; }
		/// <summary>
		/// Unique identifier for this particular command. Will be used to identify the command globally.
		/// </summary>
		string UniqueId => (CommandName.GetTypeCode().GetHashCode() + CommandName.GetHashCode() + MaxParameterCount).ToString()
							+ "_" + CommandDescription.GetHashCode().ToString();
		/// <summary>
		/// Short description containing what the command does
		/// </summary>
		string CommandDescription { get; }
		/// <summary>
		/// The key value which will be used to match against the input of the user while parsing.
		/// </summary>
		string CommandKey { get; }
		/// <summary>
		/// The Unique command code specifying this command globally.
		/// </summary>
		COMMAND_CODE CommandCode { get; }

		/// <summary>
		/// For quick parsing of the command and checking if the inputted command is for the current command context
		/// </summary>
		/// <param name="command">The inputted command</param>
		/// <param name="paramsCount">The inputted command parameter count</param>
		/// <returns>Boolean indicating if its the current context or not</returns>
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

		/// <summary>
		/// Used to parse the command and see if it is actually the specified command for the current context.
		/// </summary>
		/// <param name="parameter">Contains the raw command returned from the input</param>
		/// <returns>Boolean value indicating if its the specified command in the current context</returns>
		bool Parse(Parameter parameter);
		/// <summary>
		/// Executes the command and prints the output onto the Standard output stream. (Console Window)
		/// </summary>
		/// <param name="parameter">The parsed parameter values</param>
		/// <returns></returns>
		Task ExecuteAsync(Parameter parameter);
		/// <summary>
		/// Initializes the command instance
		/// </summary>
		/// <returns></returns>
		Task InitAsync();
		/// <summary>
		/// The Function to execute right before the command execution occurs in the command instance. (optional) Takes in Parameter object and returns a boolean indicating the status of the Func Execution
		/// </summary>
		Func<Parameter, bool> OnExecuteFunc { get; set; }
		/// <summary>
		/// Disposes the command object.
		/// </summary>
		void Dispose();
	}
}
