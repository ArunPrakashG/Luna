namespace Luna {
	using Luna.Server;
	using Luna.Shell;
	using Luna.Update;
	using Luna.Watchers;
	using Luna.Watchers.Interfaces;
	using Luna.Extensions;
	using Luna.Gpio;
	using Luna.Gpio.Drivers;
	using Luna.Logging;
	using Luna.Logging.Interfaces;
	using Luna.Modules;
	using Luna.Modules.Interfaces.EventInterfaces;
	using Luna.Sound.Speech;
	using FluentScheduler;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Unosquare.RaspberryIO;
	using static Luna.Gpio.Enums;
	using static Luna.Logging.Enums;
	using static Luna.Modules.ModuleInitializer;

	public class Core {
		private readonly ILogger Logger = new Logger(nameof(Core));
		private readonly CancellationTokenSource KeepAliveToken = new CancellationTokenSource();
		private readonly IWatcher InternalFileWatcher;
		private readonly IWatcher InternalModuleWatcher;
		private readonly GpioCore Controller;
		private readonly UpdateManager Updater;
		private readonly CoreConfig Config;
		private readonly ModuleInitializer ModuleLoader;
		private readonly DateTime StartupTime;
		private readonly RestCore RestServer;

		internal readonly bool IsBaseInitiationCompleted;
		internal readonly bool DisableFirstChanceLogWithDebug;
		internal readonly bool InitiationCompleted;

		public bool IsNetworkAvailable { get; internal set; }

		public string AssistantName {
			get => Config.AssistantDisplayName ?? "Home Assistant";
			internal set => Config.AssistantDisplayName = value ?? Config.AssistantDisplayName;
		}

		internal Core(string[] args) {
			OS.Init(true);
			Console.Title = $"Home Assistant Initializing...";
			StartupTime = DateTime.Now;
			File.WriteAllText("version.txt", Constants.Version?.ToString());

			if (File.Exists(Constants.TraceLogPath)) {
				File.Delete(Constants.TraceLogPath);
			}

			JobManager.Initialize(new Registry());

			Config = new CoreConfig(this);
			Config.Load();
			Config.ProgramLastStartup = StartupTime;

			Helpers.SetFileSeperator();
			IsNetworkAvailable = Helpers.IsNetworkAvailable();
			Constants.LocalIP = Helpers.GetLocalIpAddress() ?? "-Invalid-";
			Constants.ExternelIP = Helpers.GetExternalIp() ?? "-Invalid-";

			if (!IsNetworkAvailable) {
				Logger.Log("No Internet connection.", LogLevels.Warn);
				Logger.Log($"Starting {AssistantName} in offline mode...");
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
			Helpers.ASCIIFromText(Config.AssistantDisplayName);
			Logger.WithColor($"X---------------- Starting {AssistantName} v{Constants.Version} ----------------X", ConsoleColor.Blue);
			IsBaseInitiationCompleted = true;
			PostInitiation().Wait();

			InternalFileWatcher = new GenericWatcher(this, "*.json", Constants.ConfigDirectory, false, null, new Dictionary<string, Action<string>>(3) {
				{ "Assistant.json", OnCoreConfigChangeEvent },
				{ "DiscordBot.json", OnDiscordConfigChangeEvent },
				{ "MailConfig.json", OnMailConfigChangeEvent }
			});

			InternalModuleWatcher = new GenericWatcher(this, "*.dll", Constants.ModuleDirectory, false, null, new Dictionary<string, Action<string>>(1) {
				{ "*", OnModuleDirectoryChangeEvent }
			});

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
				() => InternalFileWatcher.StopWatcher(),
				() => InternalModuleWatcher.StopWatcher(),
				() => ModuleLoader?.OnCoreShutdown(),
				() => Config.Save()
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
			Logging.NLog.LoggerOnShutdown();
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

		internal IWatcher GetFileWatcher() => InternalFileWatcher;

		internal IWatcher GetModuleWatcher() => InternalModuleWatcher;
	}
}
