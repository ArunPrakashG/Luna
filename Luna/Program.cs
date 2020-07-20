using Luna.Extensions;
using Luna.Logging;
using Luna.Logging.Interfaces;
using System;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Logging.Enums;

namespace Luna.Core {	
	public class Program {
		private static readonly ILogger Logger = new Logger(typeof(Program).Name);
		private static Mutex? InstanceIdentifierMutex;

		internal static Core CoreInstance;

		private static async Task Main(string[] args) {
			const string _mutexName = "HomeAssistant";
			InstanceIdentifierMutex = new Mutex(false, _mutexName);

			Logger.Warning("Trying to acquire instance Mutex...");
			bool mutexAcquired = false;

			try {
				mutexAcquired = InstanceIdentifierMutex.WaitOne(60000);
			}
			catch (AbandonedMutexException) {
				mutexAcquired = true;
			}

			if (!mutexAcquired) {
				Logger.Error("Failed to acquire instance mutex.");
				Logger.Error("You might be running multiple instances of the same process.");
				Logger.Error("Running multiple instances can cause unavoidable errors. Exiting now...");
				await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				return;
			}

			InstanceIdentifierMutex.WaitOne();

			try {
				CoreEventManager.Init();

				TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
				AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
				AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
				NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
				AppDomain.CurrentDomain.ProcessExit += OnEnvironmentExit;
				Console.CancelKeyPress += OnForceQuitAssistant;

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
					"clear".ExecuteBash(false);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					Console.Clear();
					Console.ResetColor();
				}

				// All init processes takes place in this constructor
				CoreInstance = new Core(args);

				//Finally, call the blocking async method to wait endlessly unless its interrupted or canceled manually.
				await CoreInstance.KeepAlive().ConfigureAwait(false);
			}
			finally {
				InstanceIdentifierMutex.ReleaseMutex();
			}
		}

		private static void OnForceQuitAssistant(object? sender, ConsoleCancelEventArgs e) => CoreInstance.Exit(-1);

		public static void HandleTaskExceptions(object? sender, UnobservedTaskExceptionEventArgs e) {
			if (sender == null || e == null || e.Exception == null) {
				return;
			}

			Logger.Log($"{e.Exception}", LogLevels.Trace);
			e.SetObserved();
		}

		public static void HandleFirstChanceExceptions(object? sender, FirstChanceExceptionEventArgs e) => Logger.Trace(e.Exception.Message);

		private static void HandleUnhandledExceptions(object? sender, UnhandledExceptionEventArgs e) {
			Logger.Log(e.ExceptionObject as Exception);

			if (e.IsTerminating) {
				CoreInstance.Exit(-1);
			}
		}

		private static void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) {
			if (e.IsAvailable && !CoreInstance.IsNetworkAvailable) {
				Logger.Log("Network is back online, reconnecting!");
				CoreInstance.OnNetworkReconnected();
				return;
			}

			if (!e.IsAvailable && CoreInstance.IsNetworkAvailable) {
				Logger.Log("Internet connection has been disconnected or disabled.", LogLevels.Error);
				Logger.Log("Disconnecting all methods which uses a stable Internet connection in order to prevent errors.", LogLevels.Error);
				CoreInstance.OnNetworkDisconnected();
				return;
			}
		}

		private static void OnEnvironmentExit(object? sender, EventArgs e) {

		}
	}
}
