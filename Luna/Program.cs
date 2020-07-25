using Luna.Logging;
using System;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Luna {
	public class Program {
		private static readonly InternalLogger Logger = new InternalLogger(typeof(Program).Name);
		private static readonly SingleInstanceMutexLocker InstanceLock = new SingleInstanceMutexLocker();

		internal static Core CoreInstance;

		private static async Task Main(string[] args) {
			Logger.Trace("Trying to acquire instance Mutex...");

			try {
				if (!InstanceLock.TryAquireLock()) {
					Logger.Error("Failed to acquire instance mutex.");
					Logger.Error("You might be running multiple instances of the same process.");
					Logger.Error("Running multiple instances can cause unavoidable errors. Exiting now...");
					await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
					return;
				}

				Logger.Info("Initializing...");
				await Init(args).ConfigureAwait(false);
			}
			catch (TimeoutException) {
				Logger.Error("Unrecoverable error occured.");
				Logger.Error("Exiting...");
				await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
				return;
			}
			finally {
				InstanceLock?.Dispose();
			}
		}

		private static async Task Init(string[] args) {
			TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
			AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
			NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
			AppDomain.CurrentDomain.ProcessExit += OnEnvironmentExit;
			Console.CancelKeyPress += OnForceQuitAssistant;

			// All init processes takes place in this constructor
			CoreInstance = new Core(args);

			//Finally, call the blocking async method to wait endlessly unless its interrupted or canceled manually.
			await CoreInstance.KeepAlive().ConfigureAwait(false);
		}

		private static void OnForceQuitAssistant(object? sender, ConsoleCancelEventArgs e) => CoreInstance.Exit(-1);

		public static void HandleTaskExceptions(object? sender, UnobservedTaskExceptionEventArgs e) {
			if (sender == null || e == null || e.Exception == null) {
				return;
			}

			Logger.Exception(e.Exception);
			e.SetObserved();
		}

		public static void HandleFirstChanceExceptions(object? sender, FirstChanceExceptionEventArgs e) => Logger.Trace(e.Exception.Message);

		private static void HandleUnhandledExceptions(object? sender, UnhandledExceptionEventArgs e) {
			Logger.Exception(e.ExceptionObject as Exception);

			if (e.IsTerminating) {
				CoreInstance.Exit(-1);
			}
		}

		private static void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) {

		}

		private static void OnEnvironmentExit(object? sender, EventArgs e) {

		}
	}
}
