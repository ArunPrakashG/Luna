
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.AssistantCore;
using Assistant.Log;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using TaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace Assistant {

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

		private static async void OnForceQuitAssistant(object? sender, ConsoleCancelEventArgs e) => await Core.Exit(-1).ConfigureAwait(false);

		public static void HandleTaskExceptions(object? sender, UnobservedTaskExceptionEventArgs e) {
			if(sender == null || e == null || e.Exception == null) {
				return;
			}

			Logger.Log($"{e.Exception.ToString()}", Enums.LogLevels.Trace);
		}

		public static void HandleFirstChanceExceptions(object? sender, FirstChanceExceptionEventArgs e) {
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

		private static void HandleUnhandledExceptions(object? sender, UnhandledExceptionEventArgs e) {
			Logger.Log((Exception) e.ExceptionObject, Enums.LogLevels.Fatal);

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
			Logger.Log("Disconnecting all methods which uses a stable internet connection in order to prevent errors.", Enums.LogLevels.Error);
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
