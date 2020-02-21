using Assistant.Extensions.Shared.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.Commands {
	public class HelpCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Help Command";

		public string CommandKey => "help";

		public string CommandDescription => "Displays information of a certain specified command.";

		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public int MaxParameterCount => 1;
		public ShellEnum.COMMAND_CODE CommandCode => ShellEnum.COMMAND_CODE.HELP_ADVANCED;

		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		public void Dispose() {

		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (parameter.Parameters == null || parameter.Parameters.Length <= 0) {
				return;
			}

			if (OnExecuteFunc != null) {
				if (OnExecuteFunc.Invoke(parameter)) {
					return;
				}
			}
		}

		public async Task InitAsync() {
			throw new NotImplementedException();
		}

		public bool Parse(Parameter parameter) {
			throw new NotImplementedException();
		}
	}
}
