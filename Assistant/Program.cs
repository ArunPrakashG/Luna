using Assistant.AssistantCore;
using Assistant.Log;
using System;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using TaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace Assistant {

	public class Program {
		private static readonly Logger Logger = new Logger("MAIN");
		private static Core _Core = new Core();

		private static async Task Main(string[] args) {
			TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
			AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
			NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
			AppDomain.CurrentDomain.ProcessExit += OnEnvironmentExit;
			Console.CancelKeyPress += OnForceQuitAssistant;

			//Start assistant step-by-step.
			//NOTE: The order matters, as its going to start one by one.
			_Core = _Core.PreInitTasks()
				.RegisterEvents()
				.LoadConfiguration()
				.VariableAssignation()
				.VerifyStartupArgs(args)
				.VerifyEnvironment()
				.StartTcpServer()
				.StartConsoleTitleUpdater()
				.DisplayASCIILogo()
				.Misc()
				.StartConfigWatcher()
				.StartKestrel()
				.StartPushBulletService()
				.StartPinController()
				.StartModules()
				.CheckAndUpdate()
				.MarkInitializationCompletion();

			//Finally, call the blocking async method to wait endlessly unless its interrupted or canceled manually.
			await Core.PostInitTasks().ConfigureAwait(false);
		}

		private static async void OnForceQuitAssistant(object? sender, ConsoleCancelEventArgs e) => await Core.Exit(-1).ConfigureAwait(false);

		public static void HandleTaskExceptions(object? sender, UnobservedTaskExceptionEventArgs e) {
			if (sender == null || e == null || e.Exception == null) {
				return;
			}

			Logger.Log($"{e.Exception.ToString()}", Enums.LogLevels.Trace);
		}

		public static void HandleFirstChanceExceptions(object? sender, FirstChanceExceptionEventArgs e) {
			if (!Core.Config.Debug || Core.DisableFirstChanceLogWithDebug) {
				return;
			}

			Logger.Log(e.Exception.Message, Core.Config.EnableFirstChanceLog ? Enums.LogLevels.Error : Enums.LogLevels.Trace);
		}

		private static void HandleUnhandledExceptions(object? sender, UnhandledExceptionEventArgs e) {
			Logger.Log(e.ExceptionObject as Exception, Enums.LogLevels.Fatal);

			if (e.IsTerminating) {
				Task.Run(async () => await Core.Exit(-1).ConfigureAwait(false));
			}
		}

		private static async Task NetworkReconnect() {
			if (!Core.IsNetworkAvailable) {
				return;
			}

			Logger.Log("Network is back online, reconnecting!");
			await Core.OnNetworkReconnected().ConfigureAwait(false);
		}

		private static async Task NetworkDisconnect() {
			if (Core.IsNetworkAvailable) {
				return;
			}

			Logger.Log("Internet connection has been disconnected or disabled.", Enums.LogLevels.Error);
			Logger.Log("Disconnecting all methods which uses a stable Internet connection in order to prevent errors.", Enums.LogLevels.Error);
			await Core.OnNetworkDisconnected().ConfigureAwait(false);
		}

		private static async void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) {
			if (e.IsAvailable && !Core.IsNetworkAvailable) {
				await NetworkReconnect().ConfigureAwait(false);
				return;
			}

			if (!e.IsAvailable && Core.IsNetworkAvailable) {
				await NetworkDisconnect().ConfigureAwait(false);
				return;
			}
		}

		private static void OnEnvironmentExit(object? sender, EventArgs e) {
		}
	}
}
