namespace Assistant.Core {
	using Assistant.Core.Shell;
	using Assistant.Core.Update;
	using Assistant.Core.Watchers;
	using Assistant.Core.Watchers.Interfaces;
	using Assistant.Extensions;
	using Assistant.Extensions.Shared.Shell;
	using Assistant.Gpio;
	using Assistant.Gpio.Config;
	using Assistant.Gpio.Controllers;
	using Assistant.Gpio.Drivers;
	using Assistant.Logging;
	using Assistant.Logging.Interfaces;
	using Assistant.Modules;
	using Assistant.Modules.Interfaces;
	using Assistant.Modules.Interfaces.EventInterfaces;
	using Assistant.Pushbullet;
	using Assistant.Rest;
	using Assistant.Sound.Speech;
	using Assistant.Weather;
	using CommandLine;
	using FluentScheduler;
	using RestSharp;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Unosquare.RaspberryIO;
	using static Assistant.Gpio.Enums;
	using static Assistant.Logging.Enums;
	using static Assistant.Modules.ModuleInitializer;

	/// <summary>
	/// Defines the <see cref="Core" />
	/// </summary>
	public class Core {
		/// <summary>
		/// The ILogger instance used for logging.
		/// </summary>
		private static readonly ILogger Logger = new Logger(typeof(Core).Name);

		/// <summary>
		/// Stores all event functions/actions which are called internally.
		/// </summary>
		private static readonly CoreEventManager EventManager = new CoreEventManager();

		private static readonly SemaphoreSlim NetworkSync = new SemaphoreSlim(1, 1);

		/// <summary>
		/// The token used to run the KeepAlive() loop.
		/// When canceled, entire application shuts down.
		/// </summary>
		private static CancellationTokenSource KeepAliveToken = new CancellationTokenSource(TimeSpan.FromDays(10));

		/// <summary>
		/// The Raspberry pi Controller instance which helps to control the pi. Including the gpio pins and bluetooth/sound etc.
		/// </summary>
		public static GpioController? Controller { get; private set; }

		/// <summary>
		/// Stores the application startup time. (Time when assistant is fully initialized.)
		/// </summary>
		public static DateTime StartupTime { get; private set; }

		/// <summary>
		/// Gets the PinController sub class of the Raspberry pi controller instance.
		/// Will be null as long as controller hasn't successfully initialized.
		/// </summary>
		public static PinController PinController => GpioController.GetPinController();

		/// <summary>
		/// The update manager instance.
		/// </summary>
		[Obsolete]
		public static UpdateManager Update { get; private set; } = new UpdateManager();

		/// <summary>
		/// The core config
		/// </summary>
		public static CoreConfig Config { get; internal set; } = new CoreConfig();

		/// <summary>
		/// The modules loader.
		/// Loads external module .dll files into assistant core.
		/// </summary>
		public static readonly ModuleInitializer ModuleLoader = new ModuleInitializer();

		/// <summary>
		/// Weather Instance used to get the weather information of a location.
		/// </summary>
		public static readonly WeatherClient WeatherClient = new WeatherClient();

		/// <summary>
		/// Push-bullet instance used to send Push to various connected devices.
		/// </summary>
		public static readonly PushbulletClient PushbulletClient = new PushbulletClient();

		/// <summary>
		/// The IFileWatcher instance for watching config directory.
		/// </summary>
		public static readonly IFileWatcher FileWatcher = new GenericFileWatcher();

		/// <summary>
		/// The IFileWatcher instance for watching the module directory.
		/// </summary>
		public static readonly IModuleWatcher ModuleWatcher = new GenericModuleWatcher();

		/// <summary>
		/// The Rest HTTP server instance.
		/// </summary>
		public static readonly RestCore RestServer = new RestCore();

		/// <summary>
		/// Gets a value indicating whether Core Initiation process is Completed.
		/// </summary>
		public static bool CoreInitiationCompleted { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether is network connectivity is available.
		/// </summary>
		public static bool IsNetworkAvailable { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether First chance logging must be disabled.
		/// </summary>
		public static bool DisableFirstChanceLogWithDebug { get; private set; }

		/// <summary>
		/// The assistant name assigned by the user.
		/// </summary>
		public static string AssistantName => !string.IsNullOrEmpty(Config.AssistantDisplayName) ? Config.AssistantDisplayName : "Home Assistant";

		/// <summary>
		/// Thread blocking method to startup the post init tasks.
		/// </summary>
		/// <returns>Boolean, when the endless thread block has been interrupted, such as, on exit.</returns>
		public static async Task PostInitTasks() {
			Logger.Log("Running post-initiation tasks...", LogLevels.Trace);
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.AssistantStartup, default);
			await TTS.AssistantVoice(TTS.ESPEECH_CONTEXT.AssistantStartup).ConfigureAwait(false);
			await KeepAlive().ConfigureAwait(false);
		}

		/// <summary>
		/// Verifies the startup arguments passed when starting assistant process.
		/// </summary>
		/// <param name="args">The args<see cref="string[]"/></param>
		/// <returns>The <see cref="Core"/></returns>
		public Core VerifyStartupArgs(string[] args) {
			ParseStartupArguments(args);
			return this;
		}

		/// <summary>
		/// Registers assistant internal events.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core RegisterEvents() {
			Logging.Logger.LogMessageReceived += EventManager.OnLogMessageReceived;
			Logging.Logger.OnColoredReceived += EventManager.OnColoredReceived;
			Logging.Logger.OnErrorReceived += EventManager.OnErrorReceived;
			Logging.Logger.OnExceptionReceived += EventManager.OnExceptionOccured;
			Logging.Logger.OnInputReceived += EventManager.OnInputReceived;
			Logging.Logger.OnWarningReceived += EventManager.OnWarningReceived;
			JobManager.JobException += EventManager.JobManagerOnException;
			JobManager.JobStart += EventManager.JobManagerOnJobStart;
			JobManager.JobEnd += EventManager.JobManagerOnJobEnd;
			return this;
		}

		/// <summary>
		/// Called right before the main init process of assistant.
		/// Normally, to set internal variables and values.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core PreInitTasks() {
			if (File.Exists(Constants.TraceLogPath)) {
				File.Delete(Constants.TraceLogPath);
			}

			Helpers.SetFileSeperator();
			Helpers.CheckMultipleProcess();
			IsNetworkAvailable = Helpers.IsNetworkAvailable();

			if (!IsNetworkAvailable) {
				Logger.Log("No Internet connection.", LogLevels.Warn);
				Logger.Log($"Starting {AssistantName} in offline mode...");
			}

			OS.Init(true);
			return this;
		}

		/// <summary>
		/// Loads the core configuration from the json file.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public async Task<Core> LoadConfiguration() {
			await Config.LoadConfig().ConfigureAwait(false);
			return this;
		}

		/// <summary>
		/// Starts the internal JobManager FluentScheduler instance.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartScheduler() {
			JobManager.Initialize(new Registry());
			return this;
		}

		/// <summary>
		/// Assigns many internal variables.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core VariableAssignation() {
			Console.Title = $"Home Assistant Initializing...";
			StartupTime = DateTime.Now;
			Config.ProgramLastStartup = StartupTime;
			Constants.LocalIP = Helpers.GetLocalIpAddress() ?? "-Invalid-";
			Constants.ExternelIP = Helpers.GetExternalIp() ?? "-Invalid-";
			Controller = new GpioController(
				new AvailablePins (
					Config.OutputModePins,
					Config.InputModePins,
					Constants.BcmGpioPins,
					Config.RelayPins,
					Config.IRSensorPins,
					Config.SoundSensorPins
					), true);			
			return this;
		}

		/// <summary>
		/// Starts the assistant shell interpreter.
		/// </summary>
		/// <typeparam name="T">The type of IShellCommand object to use for loading the commands.</typeparam>
		/// <returns>The <see cref="Core"/></returns>
		public async Task<Core> InitShell<T>() where T : IShellCommand {
			Interpreter.Pause();
			await Interpreter.InitInterpreterAsync<T>().ConfigureAwait(false);
			return this;
		}

		/// <summary>
		/// Sends current application local ip to a remote server so that connecting via Local network is easier.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>		
		public Core AllowLocalNetworkConnections() {
			SendLocalIp();
			return this;
		}

		/// <summary>
		/// Starts the Console title updater job and updates the title every 1 second.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartConsoleTitleUpdater() {
			JobManager.AddJob(() => SetConsoleTitle(), (s) => s.WithName("ConsoleUpdater").ToRunEvery(1).Seconds());
			return this;
		}

		/// <summary>
		/// Displays the ASCII Logo of the assistant name loaded from the config file.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core DisplayASCIILogo() {
			Helpers.GenerateAsciiFromText(Config.AssistantDisplayName);
			Logger.WithColor($"X---------------- Starting {AssistantName} V{Constants.Version} ----------------X", ConsoleColor.Blue);
			return this;
		}

		/// <summary>
		/// Starts the Rest HTTP server of assistant with internal commands.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		[Obsolete("TODO: Add more commands")]
		public async Task<Core> InitRestServer() {
			await RestServer.InitServer(new Dictionary<string, Func<RequestParameter, RequestResponse>>() {
				{"example_command", EventManager.RestServerExampleCommand }
				}).ConfigureAwait(false);

			return this;
		}

		/// <summary>
		/// Saves version related information to version.txt file in the root directory.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core Versioning() {
			File.WriteAllText("version.txt", Constants.Version?.ToString());
			return this;
		}

		/// <summary>
		/// Starts the config watcher and module watcher instances.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartWatcher() {
			FileWatcher.InitWatcher(Constants.ConfigDirectory, new Dictionary<string, Action>() {
				{ "Assistant.json", new Action(OnCoreConfigChangeEvent) },
				{ "DiscordBot.json", new Action(OnDiscordConfigChangeEvent) },
				{ "MailConfig.json", new Action(OnMailConfigChangeEvent) }
			}, new List<string>(), "*.json", false);

			ModuleWatcher.InitWatcher(Constants.ModuleDirectory, new List<Action<string>>() {
				new Action<string>((x) => OnModuleDirectoryChangeEvent(x))
			}, new List<string>(), "*.dll", false);

			return this;
		}

		/// <summary>
		/// Starts the push bullet service.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core InitPushbulletService() {
			if (string.IsNullOrEmpty(Config.PushBulletApiKey)) {
				Logger.Trace("Push bullet API key is null or invalid.");
				return this;
			}

			PushbulletClient.InitPushbulletClient(Config.PushBulletApiKey);
			Logger.Info("Push bullet notification service started.");
			return this;
		}

		/// <summary>
		/// Checks for any update available and updates automatically if there is.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public async Task<Core> CheckAndUpdate() {
			await Update.CheckAndUpdateAsync(true).ConfigureAwait(false);
			return this;
		}

		/// <summary>
		/// Loads the modules from the module directory.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public async Task<Core> StartModules<T>() where T: IModuleBase {
			if (!Config.EnableModules) {
				return this;
			}

			await ModuleLoader.LoadAsync<T>().ConfigureAwait(false);
			return this;
		}

		/// <summary>
		/// Starts the pi controller instance.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public async Task<Core> InitPiGpioController<T>(T driver, NumberingScheme numberingScheme = NumberingScheme.Logical) where T: IGpioControllerDriver {
			if (!GpioController.IsAllowedToExecute || Controller == null) {
				Logger.Warning("Raspberry pi GPIO functions cannot run due to incompatability with the system.");
				return this;
			}

			await Controller.InitController<T>(driver, numberingScheme).ConfigureAwait(false);
			return this;
		}

		/// <summary>
		/// Marks the end of initiation process.
		/// If not called, Most of the internal functions wont work.
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core MarkInitializationCompletion() {
			CoreInitiationCompleted = true;
			Logger.Info("Core has been loaded!");
			return this;
		}

		/// <summary>
		/// Called when network is disconnected.
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task OnNetworkDisconnected() {
			try {
				await NetworkSync.WaitAsync().ConfigureAwait(false);
				IsNetworkAvailable = false;
				ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.NetworkDisconnected, default);
				Constants.ExternelIP = "Internet connection lost.";
			}
			finally {
				NetworkSync.Release();
			}
		}

		/// <summary>
		/// Called when network is reconnected.
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task OnNetworkReconnected() {
			try {
				await NetworkSync.WaitAsync().ConfigureAwait(false);
				IsNetworkAvailable = true;
				ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.NetworkReconnected, default);
				Constants.ExternelIP = Helpers.GetExternalIp();

				if (Config.AutoUpdates && IsNetworkAvailable) {
					Logger.Log("Checking for any new version...", LogLevels.Trace);
					File.WriteAllText("version.txt", Constants.Version?.ToString());
					await Update.CheckAndUpdateAsync(true).ConfigureAwait(false);
				}
			}
			finally {
				NetworkSync.Release();
			}
		}

		/// <summary>
		/// Called when assistant has been requested to exit.
		/// This method handles gracefully exiting the assistant.
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task OnExit() {
			Logger.Log("Shutting down...");

			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.AssistantShutdown, default);

			Interpreter.ExitShell();
			await RestServer.Shutdown().ConfigureAwait(false);
			Controller?.Shutdown();
			JobManager.RemoveAllJobs();
			JobManager.Stop();
			FileWatcher.StopWatcher();
			ModuleWatcher.StopWatcher();
			ModuleLoader?.OnCoreShutdown();
			Config.ProgramLastShutdown = DateTime.Now;

			await CoreConfig.SaveConfig(Config).ConfigureAwait(false);
			Logger.Log("Finished exit tasks.", LogLevels.Trace);
		}

		/// <summary>
		/// The Exit
		/// </summary>
		/// <param name="exitCode">The exitCode<see cref="int"/></param>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task Exit(int exitCode = 0) {
			if (exitCode != 0) {
				Logger.Log("Exiting with nonzero error code...", LogLevels.Error);
			}

			if (exitCode == 0) {
				await OnExit().ConfigureAwait(false);
			}

			Logger.Log("Bye, have a good day sir!");
			NLog.NLog.LoggerOnShutdown();
			KeepAliveToken.Cancel();
			Environment.Exit(exitCode);
		}

		/// <summary>
		/// The Restart
		/// </summary>
		/// <param name="delay">The delay<see cref="int"/></param>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task Restart(int delay = 10) {
			Helpers.ScheduleTask(() => "cd /home/pi/Desktop/HomeAssistant/Helpers/Restarter && dotnet RestartHelper.dll".ExecuteBash(false), TimeSpan.FromSeconds(delay));
			await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
			await Exit(0).ConfigureAwait(false);
		}

		/// <summary>
		/// The SystemShutdown
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task SystemShutdown() {
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.SystemShutdown, default);
			if (GpioController.IsAllowedToExecute) {
				Logger.Log($"Assistant is running on raspberry pi.", LogLevels.Trace);
				Logger.Log("Shutting down pi...", LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				await Pi.ShutdownAsync().ConfigureAwait(false);
				return;
			}

			if (Helpers.GetOsPlatform() == OSPlatform.Windows) {
				Logger.Log($"Assistant is running on a windows system.", LogLevels.Trace);
				Logger.Log("Shutting down system...", LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /t 0") {
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(psi);
			}
		}

		/// <summary>
		/// The SystemRestart
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task SystemRestart() {
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.SystemRestart, default);
			if (GpioController.IsAllowedToExecute) {
				Logger.Log($"Assistant is running on raspberry pi.", LogLevels.Trace);
				Logger.Log("Restarting pi...", LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				await Pi.RestartAsync().ConfigureAwait(false);
				return;
			}

			if (Helpers.GetOsPlatform() == OSPlatform.Windows) {
				Logger.Log($"Assistant is running on a windows system.", LogLevels.Trace);
				Logger.Log("Restarting system...", LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/r /t 0") {
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(psi);
			}
		}

		private static async Task KeepAlive() {
			Logger.Log($"Press {Constants.SHELL_KEY} for shell execution.", LogLevels.Green);
			while (!KeepAliveToken.Token.IsCancellationRequested) {
				try {
					if (Interpreter.PauseShell) {
						char pressedKey = Console.ReadKey().KeyChar;

						switch (pressedKey) {
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

		private static void ParseStartupArguments(string[] args) {
			if (!args.Any() || args == null) {
				return;
			}

			Parser.Default.ParseArguments<StartupOptions>(args).WithParsed(x => {
				if (x.Debug) {
					Logger.Log("Debug mode enabled. Logging trace data to console.", LogLevels.Warn);
					Config.Debug = true;
				}

				if (x.Safe) {
					Logger.Log("Safe mode enabled. Only preconfigured gpio pins are allowed to be modified.", LogLevels.Warn);
					Config.GpioSafeMode = true;
				}

				if (x.DisableFirstChance) {
					Logger.Log("Disabling first chance exception logging with debug mode.", LogLevels.Warn);
					DisableFirstChanceLogWithDebug = true;
				}
			});
		}

		private void OnCoreConfigChangeEvent() {
			if (!File.Exists(Constants.CoreConfigPath)) {
				Logger.Log("The core config file has been deleted.", LogLevels.Warn);
				Logger.Log("Fore quitting assistant.", LogLevels.Warn);
				Task.Run(async () => await Exit(0).ConfigureAwait(false));
			}

			Logger.Log("Updating core config as the local config file as been updated...");
			Helpers.InBackgroundThread(async () => await Config.LoadConfig().ConfigureAwait(false));
		}

		private void OnDiscordConfigChangeEvent() {
		}

		private void OnMailConfigChangeEvent() {
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

			Helpers.InBackground(async () => await ModuleLoader.LoadAsync<IModuleBase>().ConfigureAwait(false));
		}

		private static void SetConsoleTitle() {
			string text = $"http://{Constants.LocalIP}:9090/ | {DateTime.Now.ToLongTimeString()} | ";
			text += GpioController.IsAllowedToExecute ? $"Uptime : {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 3)} minutes" : null;
			Helpers.SetConsoleTitle(text);
		}

		/// <summary>
		/// The method sends the current working local ip to an central server which i personally use for such tasks and for authentication etc.
		/// You have to specify such a server manually else contact me personally for my server IP.
		/// We use this so that the mobile controller app of the assistant can connect to the assistant running on the connected local interface.
		/// </summary>
		private static void SendLocalIp() {
			string? localIp = Helpers.GetLocalIpAddress();

			if (string.IsNullOrEmpty(localIp)) {
				return;
			}

			Constants.LocalIP = localIp;
			int maxTry = 3;

			for (int i = 0; i < maxTry; i++) {
				RestClient client = new RestClient($"http://{Config.StatisticsServerIP}/api/v1/assistant/ip?ip={Constants.LocalIP}");
				RestRequest request = new RestRequest(Method.POST);
				request.AddHeader("cache-control", "no-cache");
				IRestResponse response = client.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK) {
					continue;
				}

				Logger.Log($"{Constants.LocalIP} IP request send!", LogLevels.Trace);
				break;
			}
		}
	}
}
