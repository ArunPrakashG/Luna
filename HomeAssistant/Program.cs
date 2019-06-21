using HomeAssistant.Core;
using HomeAssistant.Log;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant {

	public class Program {
		private static readonly Logger Logger = new Logger("CORE");
		private static bool NetworkBusy { get; set; }

		private static CancellationTokenSource EndlessAwaitCancelletionToken;

		// Handle Pre-init Tasks in here
		private static async Task Main(string[] args) {
			TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
			AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
			NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
			AppDomain.CurrentDomain.ProcessExit += OnEnvironmentExit;
			Console.CancelKeyPress += OnForceQuitTess;
			bool Init = await Tess.InitCore(args).ConfigureAwait(false);
		}

		private static async void OnForceQuitTess(object sender, ConsoleCancelEventArgs e) => await Tess.Exit(0).ConfigureAwait(false);

		public static void HandleTaskExceptions(object sender, UnobservedTaskExceptionEventArgs e) {
			Logger.Log($"{e.Exception.Message}", LogLevels.Error);
			Logger.Log($"{e.Exception.ToString()}", LogLevels.Trace);
		}

		public static void HandleFirstChanceExceptions(object sender, FirstChanceExceptionEventArgs e) {
			if (Tess.Config.Debug) {
				if (Tess.DisableFirstChanceLogWithDebug) {
					return;
				}

				if (Tess.Config.EnableFirstChanceLog) {
					if (e.Exception is PlatformNotSupportedException) {
						Logger.Log(e.Exception.Message, LogLevels.Error);
					}
					else if (e.Exception is ArgumentNullException) {
						Logger.Log(e.Exception.Message, LogLevels.Error);
					}
					else if (e.Exception is OperationCanceledException) {
						Logger.Log(e.Exception.Message, LogLevels.Error);
					}
					else if (e.Exception is IOException) {
						Logger.Log(e.Exception.Message, LogLevels.Error);
					}
					else {
						Logger.Log(e.Exception.Message, LogLevels.Error);
					}
				}
				else {
					if (e.Exception is PlatformNotSupportedException) {
						Logger.Log("Platform not supported exception thrown.", LogLevels.Trace);
					}
					else if (e.Exception is ArgumentNullException) {
						Logger.Log("Argument null exception thrown.", LogLevels.Trace);
					}
					else if (e.Exception is OperationCanceledException) {
						Logger.Log("Operation cancelled exception thrown.", LogLevels.Trace);
					}
					else if (e.Exception is IOException) {
						Logger.Log("IO Exception thrown.", LogLevels.Trace);
					}
					else {
						Logger.Log(e.Exception.Message, LogLevels.Trace);
					}
				}
			}
		}

		private static void HandleUnhandledExceptions(object sender, UnhandledExceptionEventArgs e) {
			Logger.Log((Exception) e.ExceptionObject, LogLevels.Fatal);

			if (e.IsTerminating) {
				Task.Run(async () => await Tess.Exit(1).ConfigureAwait(false));
			}
		}

		private static async Task NetworkReconnect() {
			Logger.Log("Network is back online, reconnecting!");
			NetworkBusy = true;
			EndlessAwaitCancelletionToken = new CancellationTokenSource();
			await Tess.OnNetworkReconnected().ConfigureAwait(false);
			EndlessAwaitCancelletionToken?.Cancel();
			NetworkBusy = false;
		}

		private static async Task NetworkDisconnect() {
			Logger.Log("Internet connection has been disconnected or disabled.", LogLevels.Error);
			Logger.Log("Disconnecting all methods which uses a stable internet connection in order to prevent errors.", LogLevels.Error);
			NetworkBusy = true;
			EndlessAwaitCancelletionToken = new CancellationTokenSource();
			await Tess.OnNetworkDisconnected().ConfigureAwait(false);
			EndlessAwaitCancelletionToken?.Cancel();
			NetworkBusy = false;
		}

		private static async void AvailabilityChanged(object sender, NetworkAvailabilityEventArgs e) {
			if (NetworkBusy) {
				if (EndlessAwaitCancelletionToken != null) {
					try {
						await Task.Delay(-1, EndlessAwaitCancelletionToken.Token).ConfigureAwait(false);

						if (e.IsAvailable && Tess.IsNetworkAvailable) {
							return;
						}
						else if (!e.IsAvailable && !Tess.IsNetworkAvailable) {
							return;
						}
						else if (e.IsAvailable && !Tess.IsNetworkAvailable) {
							await NetworkReconnect().ConfigureAwait(false);
						}
						else if (!e.IsAvailable && Tess.IsNetworkAvailable) {
							await NetworkDisconnect().ConfigureAwait(false);
						}
						else {
							return;
						}
					}
					catch (TaskCanceledException) {
						//endless loop ended
					}
					catch (OperationCanceledException) {

					}
				}
			}

			if (e.IsAvailable) {
				await NetworkReconnect().ConfigureAwait(false);
			}
			else {
				await NetworkDisconnect().ConfigureAwait(false);
			}
		}

		private static void OnEnvironmentExit(object sender, EventArgs e) => Task.Run(async () => await Tess.OnExit().ConfigureAwait(false));
	}
}
