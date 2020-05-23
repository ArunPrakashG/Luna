using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using Assistant.Gpio.Drivers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Modules.Interfaces;
using System;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;

namespace Assistant.Core {
	public class Program {
		private static readonly ILogger Logger = new Logger(typeof(Program).Name);
		private static Mutex? InstanceIdentifier;

		internal static Core CoreInstance; 

		private static async Task Main(string[] args) {
			try {
				const string _identifierName = "HomeAssistant";
				if (Mutex.TryOpenExisting(_identifierName, out Mutex? _existingInstance) && _existingInstance != null) {
					Console.WriteLine("Multiple instances running...");
					Console.WriteLine("Existing current instance in 5 seconds...");
					await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
					Environment.Exit(-1);
					return;
				}
				
				InstanceIdentifier = new Mutex(true, _identifierName);
			}
			catch (Exception e) {
				Console.WriteLine("Fatal exception occurred.");
				Console.WriteLine(e);
				Console.WriteLine("Existing current instance in 20 seconds...");
				Task.Delay(TimeSpan.FromSeconds(20)).Wait();
				Environment.Exit(-1);
				return;
			}

			InstanceIdentifier.WaitOne();

			TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
			AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
			NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
			AppDomain.CurrentDomain.ProcessExit += OnEnvironmentExit;
			Console.CancelKeyPress += OnForceQuitAssistant;

			try {
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
				InstanceIdentifier.ReleaseMutex();
				InstanceIdentifier.Dispose();
			}
		}

		private static async void OnForceQuitAssistant(object? sender, ConsoleCancelEventArgs e) => await CoreInstance.Exit(-1).ConfigureAwait(false);

		public static void HandleTaskExceptions(object? sender, UnobservedTaskExceptionEventArgs e) {
			if (sender == null || e == null || e.Exception == null) {
				return;
			}

			Logger.Log($"{e.Exception}", LogLevels.Trace);
			e.SetObserved();
		}

		public static void HandleFirstChanceExceptions(object? sender, FirstChanceExceptionEventArgs e)=> Logger.Trace(e.Exception.Message);

		private static void HandleUnhandledExceptions(object? sender, UnhandledExceptionEventArgs e) {
			Logger.Log(e.ExceptionObject as Exception);			

			if (e.IsTerminating) {
				InstanceIdentifier?.ReleaseMutex();
				Task.Run(async () => await CoreInstance.Exit(-1).ConfigureAwait(false));
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
			InstanceIdentifier?.ReleaseMutex();
		}
	}
}
