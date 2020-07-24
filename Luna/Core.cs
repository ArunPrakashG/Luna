namespace Luna {
	using Figgle;
	using FluentScheduler;
	using Luna.Gpio;
	using Luna.Gpio.Drivers;
	using Luna.Logging;
	using Luna.Modules;
	using Luna.Modules.Interfaces.EventInterfaces;
	using Luna.Server;
	using Luna.Shell;
	using Luna.Sound.Speech;
	using Luna.Update;
	using Luna.Watchers;
	using Luna.Watchers.Interfaces;
	using Synergy.Extensions;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Unosquare.RaspberryIO;
	using static Luna.Gpio.Enums;
	using static Luna.Modules.ModuleInitializer;

	internal class Core {
		private static readonly Stopwatch RuntimeSpanCounter;
		private readonly InternalLogger Logger;
		private readonly CancellationTokenSource KeepAliveToken = new CancellationTokenSource();
		private readonly ConfigWatcher InternalConfigWatcher;
		private readonly ModuleWatcher InternalModuleWatcher;
		private readonly GpioCore Controller;
		private readonly UpdateManager Updater;
		private readonly CoreConfig Config;		
		private readonly ModuleInitializer ModuleLoader;
		private readonly RestCore RestServer;

		internal readonly bool IsBaseInitiationCompleted;
		internal readonly bool DisableFirstChanceLogWithDebug;

		internal readonly bool InitiationCompleted;

		internal static bool IsNetworkAvailable => Helpers.IsNetworkAvailable();

		static Core() {
			RuntimeSpanCounter = new Stopwatch();
			JobManager.Initialize();
		}

		internal Core(string[] args) {
			Console.Title = $"Initializing...";
			Logger = InternalLogger.GetOrCreateLogger<Core>(this, nameof(Core));
			OS.Init(true);
			RuntimeSpanCounter.Restart();			
			File.WriteAllText("version.txt", Constants.Version?.ToString());

			if (File.Exists(Constants.TraceLogPath)) {
				File.Delete(Constants.TraceLogPath);
			}

			Config = new CoreConfig(this);
			Config.LoadAsync().Wait();
			Config.LocalIP = Helpers.GetLocalIpAddress()?.ToString() ?? "-Invalid-";
			Config.PublicIP = Helpers.GetPublicIP()?.ToString() ?? "-Invalid-";

			if (!IsNetworkAvailable) {
				Logger.Warn("No Internet connection.");
				Logger.Info($"Starting offline mode...");
			}

			Controller = new GpioCore(new AvailablePins(
					Config.OutputModePins,
					Config.InputModePins,
					Constants.BcmGpioPins,
					Config.RelayPins,
					Config.IRSensorPins,
					Config.SoundSensorPins
			), true);

			Updater = new UpdateManager(this);
			ModuleLoader = new ModuleInitializer();
			RestServer = new RestCore(Config.RestServerPort, Config.Debug);

			JobManager.AddJob(() => SetConsoleTitle(), (s) => s.WithName("ConsoleUpdater").ToRunEvery(1).Seconds());
			Logger.CustomLog(FiggleFonts.Ogre.Render("LUNA"), ConsoleColor.Green);
			Logger.CustomLog($"---------------- Starting Luna v{Constants.Version} ----------------", ConsoleColor.Blue);
			IsBaseInitiationCompleted = true;
			PostInitiation().Wait();
			InternalConfigWatcher = new ConfigWatcher(this);
			InternalModuleWatcher = new ModuleWatcher(this);
			InitiationCompleted = true;
		}

		private async Task PostInitiation() {
			async void moduleLoaderAction() => await ModuleLoader.LoadAsync(Config.EnableModules).ConfigureAwait(false);

			async void checkAndUpdateAction() => await Updater.CheckAndUpdateAsync(true).ConfigureAwait(false);

			async void gpioControllerInitAction() {
				IGpioControllerDriver? _driver = default;

				switch (Config.GpioDriverProvider) {
					case GpioDriver.RaspberryIODriver:
						_driver = new RaspberryIODriver();
						break;
					case GpioDriver.SystemDevicesDriver:
						_driver = new SystemDeviceDriver();
						break;
					case GpioDriver.WiringPiDriver:
						_driver = new WiringPiDriver();
						break;
				}

				await Controller.InitController(_driver, OS.IsUnix, Config.PinNumberingScheme).ConfigureAwait(false);
			}

			async void restServerInitAction() => await RestServer.InitServerAsync().ConfigureAwait(false);

			static async void endStartupAction() {
				ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.AssistantStartup, default);
				await TTS.AssistantVoice(TTS.ESPEECH_CONTEXT.AssistantStartup).ConfigureAwait(false);
			}

			Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 10 },
				moduleLoaderAction,
				checkAndUpdateAction,
				gpioControllerInitAction,
				restServerInitAction,
				endStartupAction
			);

			Interpreter.Pause();
			await Interpreter.InitInterpreterAsync().ConfigureAwait(false);
		}

		internal void OnNetworkDisconnected() {
			IsNetworkAvailable = false;
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.NetworkDisconnected, default);
			Constants.ExternelIP = "Internet connection lost.";
		}

		internal void OnNetworkReconnected() {
			IsNetworkAvailable = true;
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.NetworkReconnected, default);
			Constants.ExternelIP = Helpers.GetExternalIp();
		}

		internal void OnExit() {
			Logger.Log("Shutting down...");
			Config.ProgramLastShutdown = DateTime.Now;

			Parallel.Invoke(
				new ParallelOptions() {
					MaxDegreeOfParallelism = 10
				},
				async () => await RestServer.ShutdownServer().ConfigureAwait(false),
				() => ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.AssistantShutdown, default),
				() => Interpreter.ExitShell(),
				() => RestServer.Dispose(),
				() => Controller?.Shutdown(),
				() => JobManager.RemoveAllJobs(),
				() => JobManager.Stop(),
				() => InternalConfigWatcher.StopWatcher(),
				() => InternalModuleWatcher.StopWatcher(),
				() => ModuleLoader?.OnCoreShutdown(),
				async () => await Config.SaveAsync().ConfigureAwait(false)
			);

			Logger.Log("Finished exit tasks.", LogLevels.Trace);
		}

		internal void Exit(int exitCode = 0) {
			if (exitCode != 0) {
				Logger.Log("Exiting with nonzero error code...", LogLevels.Error);
			}

			if (exitCode == 0) {
				OnExit();
			}

			Logger.Log("Bye, have a good day sir!");
			Logging.InternalLogManager.LoggerOnShutdown();
			KeepAliveToken.Cancel();
			Environment.Exit(exitCode);
		}

		internal async Task Restart(int delay = 10) {
			Helpers.ScheduleTask(() => "cd /home/pi/Desktop/HomeAssistant/Helpers/Restarter && dotnet RestartHelper.dll".ExecuteBash(false), TimeSpan.FromSeconds(delay));
			await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
			Exit(0);
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

		internal async Task KeepAlive() {
			Logger.Log($"Press {Constants.SHELL_KEY} for shell.", LogLevels.Green);
			while (!KeepAliveToken.Token.IsCancellationRequested) {
				try {
					if (Interpreter.PauseShell) {
						if (!Console.KeyAvailable) {
							continue;
						}

						ConsoleKeyInfo pressedKey = Console.ReadKey(true);

						switch (pressedKey.Key) {
							case Constants.SHELL_KEY:
								Interpreter.Resume();
								continue;

							default:
								continue;
						}
					}
				}
				finally {
					await Task.Delay(1).ConfigureAwait(false);
				}
			}
		}

		private void OnCoreConfigChangeEvent(string? fileName) {
			if (!File.Exists(Constants.CoreConfigPath)) {
				Logger.Log("The core config file has been deleted.", LogLevels.Warn);
				Logger.Log("Fore quitting assistant.", LogLevels.Warn);
				Exit(0);
			}

			Logger.Log("Updating core config as the local config file as been updated...");
			Helpers.InBackgroundThread(Config.Load);
		}

		private void OnDiscordConfigChangeEvent(string? fileName) {
		}

		private void OnMailConfigChangeEvent(string? fileName) {
		}

		private void OnModuleDirectoryChangeEvent(string? absoluteFileName) {
			if (string.IsNullOrEmpty(absoluteFileName)) {
				return;
			}

			string fileName = Path.GetFileName(absoluteFileName);
			string filePath = Path.GetFullPath(absoluteFileName);
			Logger.Log($"An event has been raised on module folder for file > {fileName}", LogLevels.Trace);

			if (!File.Exists(filePath)) {
				ModuleLoader.UnloadFromPath(filePath);
				return;
			}

			Helpers.InBackground(async () => await ModuleLoader.LoadAsync(Config.EnableModules).ConfigureAwait(false));
		}

		private void SetConsoleTitle() {
			string text = $"{AssistantName} v{Constants.Version} | https://{Constants.LocalIP}:{Config.RestServerPort + 1}/ | {DateTime.Now.ToLongTimeString()} | ";
			text += GpioCore.IsAllowedToExecute ? $"Uptime : {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 3)} minutes" : null;
			Helpers.SetConsoleTitle(text);
		}

		public GpioCore GetGpioCore() => Controller;

		public UpdateManager GetUpdater() => Updater;

		public CoreConfig GetCoreConfig() => Config;

		public ModuleInitializer GetModuleInitializer() => ModuleLoader;

		internal FileWatcherBase GetFileWatcher() => InternalConfigWatcher;

		internal FileWatcherBase GetModuleWatcher() => InternalModuleWatcher;
	}
}
