using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Luna {
	internal class SystemController {
		private readonly InternalLogger Logger = new InternalLogger(nameof(SystemController));
		private readonly Core Core;

		internal SystemController(Core core) {
			Core = core ?? throw new ArgumentNullException(nameof(core));
		}

		internal async Task SystemShutdown() {
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.SystemShutdown, default);
			if (GpioCore.IsAllowedToExecute) {
				Logger.Log($"Assistant is running on raspberry pi.", LogLevels.Trace);
				Logger.Log("Shutting down pi...", LogLevels.Warn);
				OnExit();
				await Pi.ShutdownAsync().ConfigureAwait(false);
				return;
			}

			if (Helpers.GetPlatform() == OSPlatform.Windows) {
				Logger.Log($"Assistant is running on a windows system.", LogLevels.Trace);
				Logger.Log("Shutting down system...", LogLevels.Warn);
				OnExit();
				ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /t 0") {
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(psi);
			}
		}

		internal async Task SystemRestart() {
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.SystemRestart, default);
			if (GpioCore.IsAllowedToExecute) {
				Logger.Log($"Assistant is running on raspberry pi.", LogLevels.Trace);
				Logger.Log("Restarting pi...", LogLevels.Warn);
				OnExit();
				await Pi.RestartAsync().ConfigureAwait(false);
				return;
			}

			if (Helpers.GetPlatform() == OSPlatform.Windows) {
				Logger.Log($"Assistant is running on a windows system.", LogLevels.Trace);
				Logger.Log("Restarting system...", LogLevels.Warn);
				OnExit();
				ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/r /t 0") {
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(psi);
			}
		}
	}
}
