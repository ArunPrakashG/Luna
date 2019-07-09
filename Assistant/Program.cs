using HomeAssistant.Log;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using HomeAssistant.AssistantCore;
using HomeAssistant.Extensions;

namespace HomeAssistant {

	public class Program {
		private static readonly Logger Logger = new Logger("MAIN");

		// Handle Pre-init Tasks in here
		private static async Task Main(string[] args) {
			TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
			AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
			NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
			AppDomain.CurrentDomain.ProcessExit += OnEnvironmentExit;
			Console.CancelKeyPress += OnForceQuitAssistant;
			bool Init = await Core.InitCore(args).ConfigureAwait(false);
		}

		private static async void OnForceQuitAssistant(object sender, ConsoleCancelEventArgs e) => await Core.Exit(-1).ConfigureAwait(false);

		public static void HandleTaskExceptions(object sender, UnobservedTaskExceptionEventArgs e) {
			Logger.Log($"{e.Exception.Message}", Enums.LogLevels.Error);
			Logger.Log($"{e.Exception.ToString()}", Enums.LogLevels.Trace);
		}

		public static void HandleFirstChanceExceptions(object sender, FirstChanceExceptionEventArgs e) {
			if (Core.Config.Debug) {
				if (Core.DisableFirstChanceLogWithDebug) {
					return;
				}

				if (Core.Config.EnableFirstChanceLog) {
					if (e.Exception is PlatformNotSupportedException) {
						Logger.Log(e.Exception.Message, Enums.LogLevels.Error);
					}
					else if (e.Exception is ArgumentNullException) {
						Logger.Log(e.Exception.Message, Enums.LogLevels.Error);
					}
					else if (e.Exception is OperationCanceledException) {
						Logger.Log(e.Exception.Message, Enums.LogLevels.Error);
					}
					else if (e.Exception is IOException) {
						Logger.Log(e.Exception.Message, Enums.LogLevels.Error);
					}
					else {
						Logger.Log(e.Exception.Message, Enums.LogLevels.Error);
					}
				}
				else {
					if (e.Exception is PlatformNotSupportedException) {
						Logger.Log("Platform not supported exception thrown.", Enums.LogLevels.Trace);
					}
					else if (e.Exception is ArgumentNullException) {
						Logger.Log("Argument null exception thrown.", Enums.LogLevels.Trace);
					}
					else if (e.Exception is OperationCanceledException) {
						Logger.Log("Operation cancelled exception thrown.", Enums.LogLevels.Trace);
					}
					else if (e.Exception is IOException) {
						Logger.Log("IO Exception thrown.", Enums.LogLevels.Trace);
					}
					else {
						Logger.Log(e.Exception.Message, Enums.LogLevels.Trace);
					}
				}
			}
		}

		private static void HandleUnhandledExceptions(object sender, UnhandledExceptionEventArgs e) {
			Logger.Log((Exception) e.ExceptionObject, Enums.LogLevels.Fatal);

			if (e.IsTerminating) {
				Task.Run(async () => await Core.Exit(-1).ConfigureAwait(false));
			}
		}

		private static async Task NetworkReconnect() {
			if(!Core.IsNetworkAvailable){
				return;
			}

			Logger.Log("Network is back online, reconnecting!");
			await Core.OnNetworkReconnected().ConfigureAwait(false);
		}

		private static async Task NetworkDisconnect() {
			if(Core.IsNetworkAvailable){
				return;
			}

			Logger.Log("Internet connection has been disconnected or disabled.", Enums.LogLevels.Error);
			Logger.Log("Disconnecting all methods which uses a stable internet connection in order to prevent errors.", Enums.LogLevels.Error);
			await Core.OnNetworkDisconnected().ConfigureAwait(false);
		}

		private static void AvailabilityChanged(object sender, NetworkAvailabilityEventArgs e) {
			float TaskId = Helpers.GenerateTaskIdentifier(new Random());
			if (e.IsAvailable) {
				if(Core.CoreInitiationCompleted){
					if(Core.TaskManager.TaskFactoryCollection.Count > 0){
						foreach(var T in Core.TaskManager.TaskFactoryCollection){
							if(T.TaskIdentifier.Equals(TaskId)){
								T.MarkAsFinsihed = true;
								Core.TaskManager.TryRemoveTask(T.TaskIdentifier);
								Logger.Log($"{T.TaskMessage} task has been removed from the task factory as connection appears to be online.");
								return;
							}
						}
					}
					Core.TaskManager.TryAddTask(new TaskStructure<Task>(){
						Task = new Task(async () => await NetworkReconnect().ConfigureAwait(false)),
						TaskIdentifier = TaskId,
						TaskMessage = "Network reconnect.",
						TimeAdded = DateTime.Now,
						MarkAsFinsihed = false,
						Delay = TimeSpan.FromSeconds(3),
						LongRunning = false
					});
				}
				return;
			}

			if (!e.IsAvailable) {
				if(Core.CoreInitiationCompleted){
					if(Core.TaskManager.TaskFactoryCollection.Count > 0){
						foreach(var T in Core.TaskManager.TaskFactoryCollection){
							if(T.TaskIdentifier.Equals(TaskId)){
								T.MarkAsFinsihed = true;
								Core.TaskManager.TryRemoveTask(T.TaskIdentifier);
								Logger.Log($"{T.TaskMessage} task has been removed from the task factory as connection appears to be offline.");
								return;
							}
						}
					}

					Core.TaskManager.TryAddTask(new TaskStructure<Task>(){
						Task = new Task(async () => await NetworkDisconnect().ConfigureAwait(false)),
						TaskIdentifier = TaskId,
						TaskMessage = "Network disconnect.",
						TimeAdded = DateTime.Now,
						MarkAsFinsihed = false,
						Delay = TimeSpan.FromSeconds(3),
						LongRunning = false
					});
				}
			}
		}

		private static void OnEnvironmentExit(object sender, EventArgs e) {
		}
	}
}
