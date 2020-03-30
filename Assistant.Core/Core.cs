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
		public static IOController? PinController => GpioController.GetPinController();

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

			if (Config.DisplayStartupMenu) {
				await DisplayRelayCycleMenu().ConfigureAwait(false);
			}

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
			StartupTime = DateTime.Now;
			Config.ProgramLastStartup = StartupTime;
			Constants.LocalIP = Helpers.GetLocalIpAddress() ?? "-Invalid-";
			Constants.ExternelIP = Helpers.GetExternalIp() ?? "-Invalid-";
			Controller = new GpioController(EGPIO_DRIVERS.WiringPiDriver,
				new AvailablePins (
					Config.OutputModePins,
					Config.InputModePins,
					Constants.BcmGpioPins,
					Config.RelayPins,
					Config.IRSensorPins,
					Config.SoundSensorPins
					), true);
			Console.Title = $"Home Assistant Initializing...";
			return this;
		}

		/// <summary>
		/// Starts the assistant shell interpreter.
		/// </summary>
		/// <typeparam name="T">The type of IShellCommand object to use for loading the commands.</typeparam>
		/// <returns>The <see cref="Core"/></returns>
		public async Task<Core> InitShell<T>() where T : IShellCommand {
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
		public Core InitPiGpioController() {
			if (!GpioController.IsAllowedToExecute || Controller == null) {
				return this;
			}

			Task.Run(async () => await Controller.InitController().ConfigureAwait(false));
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

			Interpreter.ShutdownShell = true;
			await Task.Delay(50);
			await RestServer.Shutdown();
			Controller?.Shutdown();
			JobManager.RemoveAllJobs();
			JobManager.Stop();
			FileWatcher.StopWatcher();
			ModuleWatcher.StopWatcher();
			ModuleLoader?.OnCoreShutdown();
			Config.ProgramLastShutdown = DateTime.Now;

			await Config.SaveConfig(Config);
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
			if (!Config.AutoRestart) {
				Logger.Log("Auto restart is turned off in config.", LogLevels.Warn);
				return;
			}

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

		[Obsolete]
		private static async Task DisplayConsoleCommandMenu() {
			Logger.Log("Displaying console command window", LogLevels.Trace);
			Logger.Log($"------------------------- COMMAND WINDOW -------------------------", LogLevels.Input);
			Logger.Log($"{Constants.ConsoleShutdownKey} - Shutdown assistant.", LogLevels.Input);

			if (GpioController.IsAllowedToExecute) {
				Logger.Log($"{Constants.ConsoleRelayCommandMenuKey} - Display relay pin control menu.", LogLevels.Input);
				Logger.Log($"{Constants.ConsoleRelayCycleMenuKey} - Display relay cycle control menu.", LogLevels.Input);

				if (Config.EnableModules) {
					Logger.Log($"{Constants.ConsoleModuleShutdownKey} - Invoke shutdown method on all currently running modules.", LogLevels.Input);
				}

				Logger.Log($"{Constants.ConsoleMorseCodeKey} - Morse code generator for the specified text.", LogLevels.Input);
			}

			Logger.Log($"{Constants.ConsoleTestMethodExecutionKey} - Run preconfigured test methods or tasks.", LogLevels.Input);

			if (WeatherClient != null) {
				Logger.Log($"{Constants.ConsoleWeatherInfoKey} - Get weather info of the specified location based on the pin code.", LogLevels.Input);
			}

			Logger.Log($"-------------------------------------------------------------------", LogLevels.Input);
			Logger.Log("Awaiting user input: \n", LogLevels.Input);

			int failedTriesCount = 0;
			int maxTries = 3;

			while (true) {
				if (failedTriesCount > maxTries) {
					Logger.Log($"Multiple wrong inputs. please start the command menu again  by pressing {Constants.ConsoleCommandMenuKey} key.", LogLevels.Warn);
					return;
				}

				char pressedKey = Console.ReadKey().KeyChar;

				switch (pressedKey) {
					case Constants.ConsoleShutdownKey:
						Logger.Log("Shutting down assistant...", LogLevels.Warn);
						await Task.Delay(1000).ConfigureAwait(false);
						await Exit(0).ConfigureAwait(false);
						return;

					case Constants.ConsoleRelayCommandMenuKey when GpioController.IsAllowedToExecute:
						Logger.Log("Displaying relay command menu...", LogLevels.Warn);
						DisplayRelayCommandMenu();
						return;

					case Constants.ConsoleRelayCycleMenuKey when GpioController.IsAllowedToExecute:
						Logger.Log("Displaying relay cycle menu...", LogLevels.Warn);
						await DisplayRelayCycleMenu().ConfigureAwait(false);
						return;

					case Constants.ConsoleRelayCommandMenuKey when !GpioController.IsAllowedToExecute:
					case Constants.ConsoleRelayCycleMenuKey when !GpioController.IsAllowedToExecute:
						Logger.Log("Assistant is running in an Operating system/Device which doesn't support GPIO pin controlling functionality.", LogLevels.Warn);
						return;

					case Constants.ConsoleMorseCodeKey when GpioController.IsAllowedToExecute:
						if (Controller == null) {
							return;
						}

						Logger.Log("Enter text to convert to Morse: ");
						string morseCycle = Console.ReadLine();
						MorseRelayTranslator? morseTranslator = GpioController.GetMorseTranslator();

						if (morseTranslator == null || !morseTranslator.IsTranslatorOnline) {
							Logger.Warning("Morse translator is offline or unavailable.");
							return;
						}
						
						await morseTranslator.RelayMorseCycle(morseCycle, Config.OutputModePins[0]).ConfigureAwait(false);
						return;

					case Constants.ConsoleWeatherInfoKey:
						Logger.Log("Please enter the pin code of the location: ");
						int counter = 0;

						int pinCode;
						while (true) {
							if (counter > 4) {
								Logger.Log("Failed multiple times. aborting...");
								return;
							}

							try {
								pinCode = Convert.ToInt32(Console.ReadLine());
								break;
							}
							catch {
								counter++;
								Logger.Log("Please try again!", LogLevels.Warn);
								continue;
							}
						}

						if (string.IsNullOrEmpty(Config.OpenWeatherApiKey)) {
							Logger.Warning("Weather api key cannot be null.");
							return;
						}

						if (WeatherClient == null) {
							Logger.Warning("Weather client is not initiated.");
							return;
						}

						WeatherResponse? response = await WeatherClient.GetWeather(Config.OpenWeatherApiKey, pinCode, "in").ConfigureAwait(false);

						if (response == null) {
							Logger.Warning("Failed to fetch weather response.");
							return;
						}

						Logger.Log($"------------ Weather information for {pinCode}/{response.LocationName} ------------", LogLevels.Green);

						if (response.Data != null) {
							Logger.Log($"Temperature: {response.Data.Temperature}", LogLevels.Green);
							Logger.Log($"Humidity: {response.Data.Humidity}", LogLevels.Green);
							Logger.Log($"Pressure: {response.Data.Pressure}", LogLevels.Green);
						}

						if (response.Wind != null) {
							Logger.Log($"Wind speed: {response.Wind.Speed}", LogLevels.Green);
						}

						if (response.Location != null) {
							Logger.Log($"Latitude: {response.Location.Latitude}", LogLevels.Green);
							Logger.Log($"Longitude: {response.Location.Longitude}", LogLevels.Green);
							Logger.Log($"Location name: {response.LocationName}", LogLevels.Green);
						}

						return;

					case Constants.ConsoleTestMethodExecutionKey:
						Logger.Log("Executing test methods/tasks", LogLevels.Warn);
						Logger.Log("Test method execution finished successfully!", LogLevels.Green);
						return;

					case Constants.ConsoleModuleShutdownKey when Modules.Count > 0 && Config.EnableModules:
						Logger.Log("Shutting down all modules...", LogLevels.Warn);
						ModuleLoader.OnCoreShutdown();
						return;

					case Constants.ConsoleModuleShutdownKey when Modules.Count <= 0:
						Logger.Log("There are no modules to shutdown...");
						return;

					default:
						if (failedTriesCount > maxTries) {
							Logger.Log($"Unknown key was pressed. ({maxTries - failedTriesCount} tries left)", LogLevels.Warn);
						}

						failedTriesCount++;
						continue;
				}
			}
		}

		[Obsolete("Disabled as long as testing of shell is going on.")]
		private static async Task KeepAlive() {
			//Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", LogLevels.Green);

			while (!KeepAliveToken.Token.IsCancellationRequested) {
				await Task.Delay(50).ConfigureAwait(false);
				//char pressedKey = Console.ReadKey().KeyChar;

				//switch (pressedKey) {
				//	case Constants.ConsoleCommandMenuKey:
				//		await DisplayConsoleCommandMenu().ConfigureAwait(false);
				//		break;

				//	default:
				//		Logger.Log("Unknown key pressed during KeepAlive() command", LogLevels.Trace);
				//		continue;
				//}
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

				if (x.EnableFirstChance) {
					Logger.Log("First chance exception logging is enabled.", LogLevels.Warn);
					Config.EnableFirstChanceLog = true;
				}

				if (x.TextToSpeech) {
					Logger.Log("Enabled text to speech service via startup arguments.", LogLevels.Warn);
					Config.EnableTextToSpeech = true;
				}

				if (x.DisableFirstChance) {
					Logger.Log("Disabling first chance exception logging with debug mode.", LogLevels.Warn);
					DisableFirstChanceLogWithDebug = true;
				}
			});
		}

		private static void DisplayRelayCommandMenu() {
			Logger.Log("-------------------- RELAY COMMAND MENU --------------------", LogLevels.Input);
			Logger.Log("1 | Relay pin 1", LogLevels.Input);
			Logger.Log("2 | Relay pin 2", LogLevels.Input);
			Logger.Log("3 | Relay pin 3", LogLevels.Input);
			Logger.Log("4 | Relay pin 4", LogLevels.Input);
			Logger.Log("5 | Relay pin 5", LogLevels.Input);
			Logger.Log("6 | Relay pin 6", LogLevels.Input);
			Logger.Log("7 | Relay pin 7", LogLevels.Input);
			Logger.Log("8 | Relay pin 8", LogLevels.Input);
			Logger.Log("9 | Schedule task for specified relay pin", LogLevels.Input);
			Logger.Log("0 | Exit menu", LogLevels.Input);
			Logger.Log("Press any key (between 0 - 9) for their respective option.\n", LogLevels.Input);
			ConsoleKeyInfo key = Console.ReadKey();
			Logger.Log("\n", LogLevels.Input);

			if (!int.TryParse(key.KeyChar.ToString(), out int SelectedValue)) {
				Logger.Log("Could not parse the input key. please try again!", LogLevels.Error);
				Logger.Log("Command menu closed.");
				Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", LogLevels.Green);
				return;
			}

			static void set(int pin) {
				if (Controller == null || PinController == null) {
					return;
				}

				Pin? pinStatus = IOController.GetDriver()?.GetPinConfig(pin);

				if (pinStatus == null) {
					return;
				}

				if (pinStatus.IsPinOn) {
					IOController.GetDriver()?.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					Logger.Log($"Successfully set {pin} pin to OFF.", LogLevels.Green);
				}
				else {
					IOController.GetDriver()?.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
					Logger.Log($"Successfully set {pin} pin to ON.", LogLevels.Green);
				}
			}

			switch (SelectedValue) {
				case 1:
					set(Config.RelayPins[0]);
					break;

				case 2:
					set(Config.RelayPins[1]);
					break;

				case 3:
					set(Config.RelayPins[2]);
					break;

				case 4:
					set(Config.RelayPins[3]);
					break;

				case 5:
					set(Config.RelayPins[4]);
					break;

				case 6:
					set(Config.RelayPins[5]);
					break;

				case 7:
					set(Config.RelayPins[6]);
					break;

				case 8:
					set(Config.RelayPins[7]);
					break;

				case 9:
					Logger.Log("Please enter the pin u want to configure: ", LogLevels.Input);
					string pinNumberKey = Console.ReadLine();

					if (!int.TryParse(pinNumberKey, out int pinNumber) || Convert.ToInt32(pinNumberKey) <= 0) {
						Logger.Log("Your entered pin number is incorrect. please enter again.", LogLevels.Input);

						pinNumberKey = Console.ReadLine();
						if (!int.TryParse(pinNumberKey, out pinNumber) || Convert.ToInt32(pinNumberKey) <= 0) {
							Logger.Log("Your entered pin number is incorrect again. press m for menu, and start again!", LogLevels.Input);
							return;
						}
					}

					Logger.Log("Please enter the amount of delay you want in between the task. (in minutes)", LogLevels.Input);
					string delayInMinuteskey = Console.ReadLine();
					if (!int.TryParse(delayInMinuteskey, out int delay) || Convert.ToInt32(delayInMinuteskey) <= 0) {
						Logger.Log("Your entered delay is incorrect. please enter again.", LogLevels.Input);

						delayInMinuteskey = Console.ReadLine();
						if (!int.TryParse(delayInMinuteskey, out delay) || Convert.ToInt32(delayInMinuteskey) <= 0) {
							Logger.Log("Your entered pin is incorrect again. press m for menu, and start again!", LogLevels.Input);
							return;
						}
					}

					Logger.Log("Please enter the status u want the task to configure: (0 = OFF, 1 = ON)", LogLevels.Input);

					string pinStatuskey = Console.ReadLine();
					if (!int.TryParse(pinStatuskey, out int pinStatus) || (Convert.ToInt32(pinStatuskey) != 0 && Convert.ToInt32(pinStatus) != 1)) {
						Logger.Log("Your entered pin status is incorrect. please enter again.", LogLevels.Input);

						pinStatuskey = Console.ReadLine();
						if (!int.TryParse(pinStatuskey, out pinStatus) || (Convert.ToInt32(pinStatuskey) != 0 && Convert.ToInt32(pinStatus) != 1)) {
							Logger.Log("Your entered pin status is incorrect again. press m for menu, and start again!", LogLevels.Input);
							return;
						}
					}

					if (Controller == null || PinController == null) {
						return;
					}

					var driver = IOController.GetDriver();

					if (driver == null) {
						return;
					}

					Pin status = driver.GetPinConfig(pinNumber);

					if (status == null) {
						return;
					}

					if (status.IsPinOn && pinStatus.Equals(1)) {
						Logger.Log("Pin is already configured to be in ON State. Command doesn't make any sense.");
						return;
					}

					if (!status.IsPinOn && pinStatus.Equals(0)) {
						Logger.Log("Pin is already configured to be in OFF State. Command doesn't make any sense.");
						return;
					}

					if (Config.IRSensorPins.Any() && Config.IRSensorPins.Contains(pinNumber)) {
						Logger.Log("Sorry, the specified pin is preconfigured for IR Sensor. cannot modify!");
						return;
					}

					if (!Config.RelayPins.Contains(pinNumber)) {
						Logger.Log("Sorry, the specified pin doesn't exist in the relay pin category.");
						return;
					}

					Helpers.ScheduleTask(() => {
						if (status.IsPinOn && pinStatus.Equals(0)) {
							driver.SetGpioValue(pinNumber, GpioPinMode.Output, GpioPinState.Off);
							Logger.Log($"Successfully finished execution of the task: {pinNumber} pin set to OFF.", LogLevels.Green);
						}

						if (!status.IsPinOn && pinStatus.Equals(1)) {
							driver.SetGpioValue(pinNumber, GpioPinMode.Output, GpioPinState.On);
							Logger.Log($"Successfully finished execution of the task: {pinNumber} pin set to ON.", LogLevels.Green);
						}
					}, TimeSpan.FromMinutes(delay));

					Logger.Log(
						pinStatus.Equals(0)
							? $"Successfully scheduled a task: set {pinNumber} pin to OFF"
							: $"Successfully scheduled a task: set {pinNumber} pin to ON", LogLevels.Green);
					break;
			}

			Logger.Log("Command menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", LogLevels.Green);
		}

		private static async Task DisplayRelayCycleMenu() {
			if (!GpioController.IsAllowedToExecute) {
				Logger.Log("You are running on incorrect OS or device. Pi controls are disabled.", LogLevels.Error);
				return;
			}

			Logger.Log("--------------------MODE MENU--------------------", LogLevels.Input);
			Logger.Log("1 | Relay Cycle", LogLevels.Input);
			Logger.Log("2 | Relay OneMany", LogLevels.Input);
			Logger.Log("3 | Relay OneOne", LogLevels.Input);
			Logger.Log("4 | Relay OneTwo", LogLevels.Input);
			Logger.Log("5 | Relay Single", LogLevels.Input);
			Logger.Log("6 | Relay Default", LogLevels.Input);
			Logger.Log("0 | Exit", LogLevels.Input);
			Logger.Log("Press any key (between 0 - 6) for their respective option.\n", LogLevels.Input);
			ConsoleKeyInfo key = Console.ReadKey();
			Logger.Log("\n", LogLevels.Input);

			if (!int.TryParse(key.KeyChar.ToString(), out int SelectedValue)) {
				Logger.Log("Could not parse the input key. please try again!", LogLevels.Error);
				Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for command menu.", LogLevels.Info);
				return;
			}

			if (Controller == null || PinController == null) {
				return;
			}

			bool Configured;
			var driver = IOController.GetDriver();

			if (driver == null) {
				return;
			}

			switch (SelectedValue) {
				case 1:
					Configured = await driver.RelayTestAsync(Config.RelayPins, GpioCycles.Cycle).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}

					break;

				case 2:
					Configured = await driver.RelayTestAsync(Config.RelayPins, GpioCycles.OneMany).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}

					break;

				case 3:
					Configured = await driver.RelayTestAsync(Config.RelayPins, GpioCycles.OneOne).ConfigureAwait(false);
					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 4:
					Configured = await driver.RelayTestAsync(Config.RelayPins, GpioCycles.OneTwo).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 5:
					Logger.Log("\nPlease select the channel (3, 4, 17, 2, 27, 10, 22, 9, etc): ", LogLevels.Input);
					string singleKey = Console.ReadLine();

					if (!int.TryParse(singleKey, out int selectedsingleKey)) {
						Logger.Log("Could not parse the input key. please try again!", LogLevels.Error);
						goto case 5;
					}

					Configured = await driver.RelayTestAsync(Config.RelayPins, GpioCycles.Single, selectedsingleKey).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 6:
					Configured = await driver.RelayTestAsync(Config.RelayPins, GpioCycles.Default).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 0:
					Logger.Log("Exiting from menu...", LogLevels.Input);
					return;

				default:
					goto case 0;
			}

			Logger.Log(Configured ? "Test successful!" : "Test Failed!");

			Logger.Log("Relay menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} to display command menu.");
		}

		/// <summary>
		/// The OnCoreConfigChangeEvent
		/// </summary>
		private void OnCoreConfigChangeEvent() {
			if (!File.Exists(Constants.CoreConfigPath)) {
				Logger.Log("The core config file has been deleted.", LogLevels.Warn);
				Logger.Log("Fore quitting assistant.", LogLevels.Warn);
				Task.Run(async () => await Exit(0).ConfigureAwait(false));
			}

			Logger.Log("Updating core config as the local config file as been updated...");
			Helpers.InBackgroundThread(async () => await Config.LoadConfig().ConfigureAwait(false));
		}

		/// <summary>
		/// The OnDiscordConfigChangeEvent
		/// </summary>
		private void OnDiscordConfigChangeEvent() {
		}

		/// <summary>
		/// The OnMailConfigChangeEvent
		/// </summary>
		private void OnMailConfigChangeEvent() {
		}

		/// <summary>
		/// The OnModuleDirectoryChangeEvent
		/// </summary>
		/// <param name="absoluteFileName">The absoluteFileName<see cref="string?"/></param>
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
