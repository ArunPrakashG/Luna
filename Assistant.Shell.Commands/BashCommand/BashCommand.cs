using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.Commands {
	public class BashCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Bash Command";

		public string CommandKey => "bash";

		public string CommandDescription => "Execute Bash scripts files via Assistant Shell.";

		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public int MaxParameterCount => 1;

		public ShellEnum.COMMAND_CODE CommandCode => ShellEnum.COMMAND_CODE.BASH_SCRIPT_PATH;

		public bool IsInitSuccess { get; set; }

		public SemaphoreSlim Sync { get; set; }

		public void Dispose() {
			IsInitSuccess = false;
			Sync.Dispose();
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellOut.Error("Too many arguments.");
				return;
			}

			if (parameter.Parameters == null || parameter.Parameters.Length <= 0) {
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (OnExecuteFunc != null) {
					if (OnExecuteFunc.Invoke(parameter)) {
						return;
					}
				}

				string? bashScriptPath = parameter.Parameters[0].Trim();
				if (string.IsNullOrEmpty(bashScriptPath)) {
					ShellOut.Error("Specified script file is invalid.");
					return;
				}

				if (!File.Exists(bashScriptPath)) {
					ShellOut.Error($"{bashScriptPath} doesn't exist.");
					return;
				}

				ShellOut.Info($"Executing {bashScriptPath} ...");
				ShellOut.Info(bashScriptPath.ExecuteBash(false));
			}
			catch (Exception e) {
				ShellOut.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		public async Task InitAsync() {
			Sync = new SemaphoreSlim(1, 1);
			IsInitSuccess = true;
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
