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
	using Assistant.Pushbullet;
	using Assistant.Rest;
	using Assistant.Server.CoreServer;
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
		/// Gets or sets the Logger
		/// </summary>
		public static ILogger Logger { get; set; } = new Logger(typeof(Core).Name);

		/// <summary>
		/// Defines the StartupTime
		/// </summary>
		public static DateTime StartupTime;

		/// <summary>
		/// Defines the RefreshConsoleTitleTimer
		/// </summary>
		private static Timer? RefreshConsoleTitleTimer;

		/// <summary>
		/// Defines the EventManager
		/// </summary>
		private static readonly EventManager EventManager = new EventManager();

		/// <summary>
		/// Gets the PiController
		/// </summary>
		public static PiGpioController? Controller { get; private set; }

		/// <summary>
		/// Gets the PinController
		/// </summary>
		public static PinController? PinController => PiGpioController.GetPinController();

		/// <summary>
		/// Gets the Update
		/// </summary>
		public static UpdateManager Update { get; private set; } = new UpdateManager();

		/// <summary>
		/// Gets or sets the Config
		/// </summary>
		public static CoreConfig Config { get; set; } = new CoreConfig();

		/// <summary>
		/// Gets the ModuleLoader
		/// </summary>
		public static ModuleInitializer ModuleLoader { get; private set; } = new ModuleInitializer();

		/// <summary>
		/// Gets the WeatherClient
		/// </summary>
		public static WeatherClient WeatherClient { get; private set; } = new WeatherClient();

		/// <summary>
		/// Gets the PushbulletClient
		/// </summary>
		public static PushbulletClient PushbulletClient { get; private set; } = new PushbulletClient();

		/// <summary>
		/// Gets the CoreServer
		/// </summary>
		public static CoreServerBase CoreServer { get; private set; } = new CoreServerBase();

		/// <summary>
		/// Gets the FileWatcher
		/// </summary>
		public static IFileWatcher FileWatcher { get; private set; } = new GenericFileWatcher();

		/// <summary>
		/// Gets the ModuleWatcher
		/// </summary>
		public static IModuleWatcher ModuleWatcher { get; private set; } = new GenericModuleWatcher();

		public static RestCore RestServer { get; private set; }

		/// <summary>
		/// Gets a value indicating whether CoreInitiationCompleted
		/// </summary>
		public static bool CoreInitiationCompleted { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether IsNetworkAvailable
		/// </summary>
		public static bool IsNetworkAvailable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether DisableFirstChanceLogWithDebug
		/// </summary>
		public static bool DisableFirstChanceLogWithDebug { get; set; }

		/// <summary>
		/// Gets the RunningPlatform
		/// </summary>
		public static OSPlatform RunningPlatform { get; private set; }

		/// <summary>
		/// Defines the NetworkSemaphore
		/// </summary>
		private static readonly SemaphoreSlim NetworkSemaphore = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Gets the AssistantName
		/// </summary>
		public static string AssistantName => !string.IsNullOrEmpty(Config.AssistantDisplayName) ? Config.AssistantDisplayName : "Home Assistant";

		/// <summary>
		/// Gets the KeepAliveToken
		/// </summary>
		public static CancellationTokenSource KeepAliveToken { get; private set; } = new CancellationTokenSource(TimeSpan.FromDays(10));

		/// <summary>
		/// Defines the JobRegistry
		/// </summary>
		public static readonly Registry JobRegistry = new Registry();

		/// <summary>
		/// Thread blocking method to startup the post init tasks.
		/// </summary>
		/// <returns>Boolean, when the endless thread block has been interrupted, such as, on exit.</returns>
		public static async Task PostInitTasks() {
			Logger.Log("Running post-initiation tasks...", LogLevels.Trace);
			await ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.AssistantStartup).ConfigureAwait(false);

			if (Config.DisplayStartupMenu) {
				await DisplayRelayCycleMenu().ConfigureAwait(false);
			}

			await TTS.AssistantVoice(TTS.ESPEECH_CONTEXT.AssistantStartup).ConfigureAwait(false);
			await KeepAlive().ConfigureAwait(false);
		}

		/// <summary>
		/// The VerifyStartupArgs
		/// </summary>
		/// <param name="args">The args<see cref="string[]"/></param>
		/// <returns>The <see cref="Core"/></returns>
		public Core VerifyStartupArgs(string[] args) {
			ParseStartupArguments(args);
			return this;
		}

		/// <summary>
		/// The RegisterEvents
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core RegisterEvents() {
			Logging.Logger.LogMessageReceived += EventManager.Logger_LogMessageReceived;
			Logging.Logger.OnColoredReceived += EventManager.Logger_OnColoredReceived;
			Logging.Logger.OnErrorReceived += EventManager.Logger_OnErrorReceived;
			Logging.Logger.OnExceptionReceived += EventManager.Logger_OnExceptionReceived;
			Logging.Logger.OnInputReceived += EventManager.Logger_OnInputReceived;
			Logging.Logger.OnWarningReceived += EventManager.Logger_OnWarningReceived;
			CoreServer.ServerStarted += EventManager.CoreServer_ServerStarted;
			CoreServer.ServerShutdown += EventManager.CoreServer_ServerShutdown;
			CoreServer.ClientConnected += EventManager.CoreServer_ClientConnected;
			JobManager.JobException += JobManager_JobException;
			JobManager.JobStart += JobManager_JobStart;
			JobManager.JobEnd += JobManager_JobEnd;
			return this;
		}

		/// <summary>
		/// The PreInitTasks
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
		/// The LoadConfiguration
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core LoadConfiguration() {
			Task.Run(async () => await Config.LoadConfig().ConfigureAwait(false));
			return this;
		}

		/// <summary>
		/// The StartScheduler
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartScheduler() {
			JobManager.Initialize(JobRegistry);
			return this;
		}

		/// <summary>
		/// The JobManager_JobException
		/// </summary>
		/// <param name="obj">The obj<see cref="JobExceptionInfo"/></param>
		private void JobManager_JobException(JobExceptionInfo obj) => Logger.Exception(obj.Exception);

		/// <summary>
		/// The VariableAssignation
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core VariableAssignation() {
			StartupTime = DateTime.Now;
			RunningPlatform = Helpers.GetOsPlatform();
			Config.ProgramLastStartup = StartupTime;
			Constants.LocalIP = Helpers.GetLocalIpAddress() ?? "-Invalid-";
			Constants.ExternelIP = Helpers.GetExternalIp() ?? "-Invalid-";
			Controller = new PiGpioController(Gpio.Enums.EGPIO_DRIVERS.RaspberryIODriver,
				new AvailablePins(Config.OutputModePins, Config.InputModePins, Constants.BcmGpioPins), true);
			Console.Title = $"Home Assistant Initializing...";
			return this;
		}

		/// <summary>
		/// The StartTcpServer
		/// </summary>
		/// <param name="port">The port<see cref="int"/></param>
		/// <param name="backlog">The backlog<see cref="int"/></param>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartTcpServer(int port, int backlog) {
			_ = CoreServer.StartAsync(port, backlog).Result;
			return this;
		}

		/// <summary>
		/// The InitShell
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns>The <see cref="Core"/></returns>
		public Core InitShell<T>() where T : IShellCommand {
			Task.Run(async () => await Interpreter.InitInterpreterAsync<T>().ConfigureAwait(false));
			return this;
		}

		/// <summary>
		/// The AllowLocalNetworkConnections
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core AllowLocalNetworkConnections() {
			SendLocalIp();
			return this;
		}

		/// <summary>
		/// The StartConsoleTitleUpdater
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartConsoleTitleUpdater() {
			Helpers.InBackgroundThread(() => SetConsoleTitle(), "Console Title Updater", true);
			return this;
		}

		/// <summary>
		/// The DisplayASCIILogo
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core DisplayASCIILogo() {
			Helpers.GenerateAsciiFromText(Config.AssistantDisplayName);
			return this;
		}

		[Obsolete("TODO: Add more commands")]
		public Core InitRestServer() {
			RestServer = new RestCore();
			Task.Run(async () => {				
				await RestServer.InitServer(new Dictionary<string, Func<RequestParameter, RequestResponse>>() {
				{"example_command", EventManager.RestServerExampleCommand }
				}).ConfigureAwait(false);
			});

			return this;
		}

		/// <summary>
		/// The DisplayASCIILogo
		/// </summary>
		/// <param name="text">The text<see cref="string?"/></param>
		/// <returns>The <see cref="Core"/></returns>
		public Core DisplayASCIILogo(string? text) {
			Helpers.GenerateAsciiFromText(text);
			return this;
		}

		/// <summary>
		/// The Versioning
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core Versioning() {
			File.WriteAllText("version.txt", Constants.Version?.ToString());
			Logger.WithColor($"X---------------- Starting {AssistantName} V{Constants.Version} ----------------X", ConsoleColor.Blue);
			return this;
		}

		/// <summary>
		/// The StartWatcher
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
		/// The InitPushbulletService
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
		/// The CheckAndUpdate
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core CheckAndUpdate() {
			_ = Update.CheckAndUpdateAsync(true).Result;
			return this;
		}

		/// <summary>
		/// The StartModules
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartModules() {
			if (!Config.EnableModules) {
				return this;
			}

			Task.Run(async () => await ModuleLoader.LoadAsync().ConfigureAwait(false));
			return this;
		}

		/// <summary>
		/// The StartPinController
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core StartPinController() {
			if (!PiGpioController.IsAllowedToExecute) {
				return this;
			}

			Controller?.InitController();
			return this;
		}

		/// <summary>
		/// The MarkInitializationCompletion
		/// </summary>
		/// <returns>The <see cref="Core"/></returns>
		public Core MarkInitializationCompletion() {
			CoreInitiationCompleted = true;
			Logger.Info("Core has been loaded!");
			return this;
		}

		//private static void TcpServerBase_ClientConnected(object sender, OnClientConnectedEventArgs e) {
		//	lock (ClientManagers) {
		//		ClientManagers.TryAdd(e.ClientUid, new TcpServerClientManager(e.ClientUid));
		//	}
		//}

		//private static void TcpServerBase_ServerStarted(object sender, OnServerStartedListerningEventArgs e) => Logger.Log($"TCP Server listening at {e.ListerningAddress} / {e.ServerPort}");

		//private static void TcpServerBase_ServerShutdown(object sender, OnServerShutdownEventArgs e) => Logger.Log($"TCP shutting down.");
		/// <summary>
		/// The SetConsoleTitle
		/// </summary>
		private static void SetConsoleTitle() {
			string text = $"http://{Constants.LocalIP}:9090/ | {DateTime.Now.ToLongTimeString()}";
			text += PiGpioController.IsAllowedToExecute ? $"Uptime : {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 3)} minutes" : null;

			Helpers.SetConsoleTitle(text);

			if (RefreshConsoleTitleTimer == null) {
				RefreshConsoleTitleTimer = new Timer(e => SetConsoleTitle(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
			}
		}

		/// <summary>
		/// The StopConsoleRefresh
		/// </summary>
		public void StopConsoleRefresh() {
			if (RefreshConsoleTitleTimer != null) {
				RefreshConsoleTitleTimer.Dispose();
				RefreshConsoleTitleTimer = null;
			}
		}

		/// <summary>
		/// The DisplayConsoleCommandMenu
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		private static async Task DisplayConsoleCommandMenu() {
			Logger.Log("Displaying console command window", LogLevels.Trace);
			Logger.Log($"------------------------- COMMAND WINDOW -------------------------", LogLevels.Input);
			Logger.Log($"{Constants.ConsoleShutdownKey} - Shutdown assistant.", LogLevels.Input);

			if (PiGpioController.IsAllowedToExecute) {
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

					case Constants.ConsoleRelayCommandMenuKey when PiGpioController.IsAllowedToExecute:
						Logger.Log("Displaying relay command menu...", LogLevels.Warn);
						DisplayRelayCommandMenu();
						return;

					case Constants.ConsoleRelayCycleMenuKey when PiGpioController.IsAllowedToExecute:
						Logger.Log("Displaying relay cycle menu...", LogLevels.Warn);
						await DisplayRelayCycleMenu().ConfigureAwait(false);
						return;

					case Constants.ConsoleRelayCommandMenuKey when !PiGpioController.IsAllowedToExecute:
					case Constants.ConsoleRelayCycleMenuKey when !PiGpioController.IsAllowedToExecute:
						Logger.Log("Assistant is running in an Operating system/Device which doesn't support GPIO pin controlling functionality.", LogLevels.Warn);
						return;

					case Constants.ConsoleMorseCodeKey when PiGpioController.IsAllowedToExecute:
						if (Controller == null) {
							return;
						}

						Logger.Log("Enter text to convert to Morse: ");
						string morseCycle = Console.ReadLine();
						GpioMorseTranslator? morseTranslator = PiGpioController.GetMorseTranslator();

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

					case Constants.ConsoleModuleShutdownKey when ModuleLoader.Modules.Count > 0 && Config.EnableModules:
						Logger.Log("Shutting down all modules...", LogLevels.Warn);
						ModuleLoader.OnCoreShutdown();
						return;

					case Constants.ConsoleModuleShutdownKey when ModuleLoader.Modules.Count <= 0:
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

		/// <summary>
		/// The KeepAlive
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
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

		/// <summary>
		/// The ParseStartupArguments
		/// </summary>
		/// <param name="args">The args<see cref="string[]"/></param>
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

		/// <summary>
		/// The DisplayRelayCommandMenu
		/// </summary>
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

				Pin? pinStatus = PinController.GetDriver()?.GetPinConfig(pin);

				if (pinStatus == null) {
					return;
				}

				if (pinStatus.IsPinOn) {
					PinController.GetDriver()?.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					Logger.Log($"Successfully set {pin} pin to OFF.", LogLevels.Green);
				}
				else {
					PinController.GetDriver()?.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
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

					var driver = PinController.GetDriver();

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

		/// <summary>
		/// The DisplayRelayCycleMenu
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		private static async Task DisplayRelayCycleMenu() {
			if (!PiGpioController.IsAllowedToExecute) {
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
			var driver = PinController.GetDriver();

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
		/// The OnNetworkDisconnected
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task OnNetworkDisconnected() {
			try {
				await NetworkSemaphore.WaitAsync().ConfigureAwait(false);
				IsNetworkAvailable = false;
				await ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.NetworkDisconnected).ConfigureAwait(false);
				Constants.ExternelIP = "Internet connection lost.";
			}
			finally {
				NetworkSemaphore.Release();
			}
		}

		/// <summary>
		/// The OnNetworkReconnected
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task OnNetworkReconnected() {
			try {
				await NetworkSemaphore.WaitAsync().ConfigureAwait(false);
				IsNetworkAvailable = true;
				await ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.NetworkReconnected).ConfigureAwait(false);
				Constants.ExternelIP = Helpers.GetExternalIp();

				if (Config.AutoUpdates && IsNetworkAvailable) {
					Logger.Log("Checking for any new version...", LogLevels.Trace);
					File.WriteAllText("version.txt", Constants.Version?.ToString());
					await Update.CheckAndUpdateAsync(true).ConfigureAwait(false);
				}
			}
			finally {
				NetworkSemaphore.Release();
			}
		}

		/// <summary>
		/// The JobManager_JobEnd
		/// </summary>
		/// <param name="obj">The obj<see cref="JobEndInfo"/></param>
		private void JobManager_JobEnd(JobEndInfo obj) {
			Logger.Trace($"A job has ended -> {obj.Name} / {obj.StartTime.ToString()}");
		}

		/// <summary>
		/// The JobManager_JobStart
		/// </summary>
		/// <param name="obj">The obj<see cref="JobStartInfo"/></param>
		private void JobManager_JobStart(JobStartInfo obj) {
			Logger.Trace($"A job has started -> {obj.Name} / {obj.StartTime.ToString()}");
		}

		/// <summary>
		/// The OnExit
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public static async Task OnExit() {
			Logger.Log("Shutting down...");

			if (ModuleLoader != null) {
				await ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.AssistantShutdown).ConfigureAwait(false);
			}

			Controller?.Shutdown();
			JobManager.RemoveAllJobs();
			JobManager.Stop();
			RefreshConsoleTitleTimer?.Dispose();
			FileWatcher.StopWatcher();
			ModuleWatcher.StopWatcher();

			if (CoreServer.IsServerListerning) {
				await CoreServer.TryShutdownAsync().ConfigureAwait(false);
			}

			//if (KestrelServer.IsServerOnline) {
			//	await KestrelServer.Stop().ConfigureAwait(false);
			//}

			ModuleLoader?.OnCoreShutdown();
			Interpreter.ShutdownShell = true;
			Config.ProgramLastShutdown = DateTime.Now;
			await Config.SaveConfig(Config).ConfigureAwait(false);
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
			await ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.SystemShutdown).ConfigureAwait(false);
			if (PiGpioController.IsAllowedToExecute) {
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
			await ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.SystemRestart).ConfigureAwait(false);
			if (PiGpioController.IsAllowedToExecute) {
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

			Helpers.InBackground(async () => await ModuleLoader.LoadAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// The method sends the current working local ip to an central server which i personally use for such tasks and for authentication etc.
		/// You have to specify such a server manually else contact me personally for my server IP.
		/// We use this so that the mobile controller app of the assistant can connect to the assistant running on the connected local interface.
		/// </summary>
		private static void SendLocalIp() {
			string? localIp = Helpers.GetLocalIpAddress();

			if (localIp == null || string.IsNullOrEmpty(localIp)) {
				return;
			}

			Constants.LocalIP = localIp;
			RestClient client = new RestClient($"http://{Config.StatisticsServerIP}/api/v1/assistant/ip?ip={Constants.LocalIP}");
			RestRequest request = new RestRequest(Method.POST);
			request.AddHeader("cache-control", "no-cache");
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
			}

			Logger.Log($"{Constants.LocalIP} IP request send!", LogLevels.Trace);
		}
	}
}
