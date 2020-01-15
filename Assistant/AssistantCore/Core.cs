using Assistant.Alarm;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Geolocation;
using Assistant.Log;
using Assistant.Modules;
using Assistant.MorseCode;
using Assistant.PushBullet;
using Assistant.Remainders;
using Assistant.Servers.Kestrel;
using Assistant.Update;
using Assistant.Weather;
using AssistantSharedLibrary.Assistant.Servers.TCPServer;
using AssistantSharedLibrary.Assistant.Servers.TCPServer.EventArgs;
using AssistantSharedLibrary.Logging;
using AssistantSharedLibrary.Logging.EventArgs;
using CommandLine;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Logging = Assistant.Log.Logging;

namespace Assistant.AssistantCore {

	public class Options {

		[Option('d', "debug", Required = false, HelpText = "Displays all Trace level messages to console. (for debugging)")]
		public bool Debug { get; set; }

		[Option('s', "safe", Required = false, HelpText = "Enables safe mode so that only preconfigured pins can be modified.")]
		public bool Safe { get; set; }

		[Option('f', "firstchance", Required = false, HelpText = "Enables logging of first chance exceptions to console.")]
		public bool EnableFirstChance { get; set; }

		[Option('t', "tts", Required = false, HelpText = "Enable text to speech system for assistant.")]
		public bool TextToSpeech { get; set; }

		[Option("df", Required = false, HelpText = "Disable first chance exception logging when debug mode is enabled.")]
		public bool DisableFirstChance { get; set; }
	}

	public class Core {
		private const int SendIpDelay = 60;
		public static Logger Logger { get; set; } = new Logger("ASSISTANT");
		public static PiController? PiController { get; private set; } = new PiController();
		public static Updater Update { get; private set; } = new Updater();
		public static CoreConfig Config { get; set; } = new CoreConfig();
		public static ConfigWatcher ConfigWatcher { get; private set; } = new ConfigWatcher();
		private static ModuleWatcher ModuleWatcher { get; set; } = new ModuleWatcher();
		public static TaskScheduler TaskManager { get; private set; } = new TaskScheduler();
		public static ModuleInitializer ModuleLoader { get; private set; } = new ModuleInitializer();
		public static ServerBase TcpServerBase { get; private set; } = new ServerBase();
		public static DateTime StartupTime { get; private set; }
		private static Timer? RefreshConsoleTitleTimer { get; set; }
		public static DynamicWatcher DynamicWatcher { get; set; } = new DynamicWatcher();
		public static WeatherApi WeatherApi { get; private set; } = new WeatherApi();
		public static ZipCodeLocater ZipCodeLocater { get; private set; } = new ZipCodeLocater();
		public static PushBulletService PushBulletService { get; private set; } = new PushBulletService();
		public static MorseCore MorseCode { get; private set; } = new MorseCore();
		public static RemainderManager RemainderManager { get; private set; } = new RemainderManager();
		public static AlarmManager AlarmManager { get; private set; } = new AlarmManager();
		public static readonly ConcurrentDictionary<string, TcpServerClientManager> ClientManagers = new ConcurrentDictionary<string, TcpServerClientManager>();

		public static bool CoreInitiationCompleted { get; private set; }
		public static bool DisablePiMethods { get; private set; }
		public static bool IsUnknownOs { get; set; }
		public static bool IsNetworkAvailable { get; set; }
		public static bool DisableFirstChanceLogWithDebug { get; set; }
		public static OSPlatform RunningPlatform { get; private set; }
		private static readonly SemaphoreSlim NetworkSemaphore = new SemaphoreSlim(1, 1);
		public static string AssistantName { get; set; } = "Tess Home Assistant";
		public static CancellationTokenSource KeepAliveToken { get; private set; } = new CancellationTokenSource(TimeSpan.MaxValue);

		/// <summary>
		/// Thread blocking method to startup the post init tasks.
		/// </summary>
		/// <returns>Boolean, when the endless thread block has been interrupted, such as, on exit.</returns>
		public static async Task PostInitTasks() {
			Logger.Log("Running post-initiation tasks...", Enums.LogLevels.Trace);
			await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.AssistantStartup).ConfigureAwait(false);

			if (Config.DisplayStartupMenu) {
				await DisplayRelayCycleMenu().ConfigureAwait(false);
			}

			await TTS.AssistantVoice(Enums.SpeechContext.AssistantStartup).ConfigureAwait(false);
			await KeepAlive().ConfigureAwait(false);
		}

		public Core VerifyStartupArgs(string[] args) {
			ParseStartupArguments(args);
			return this;
		}

		public Core RegisterEvents() {
			EventLogger.LogMessageReceived += EventLogger_LogMessageReceived;
			TcpServerBase.ServerShutdown += TcpServerBase_ServerShutdown;
			TcpServerBase.ServerStarted += TcpServerBase_ServerStarted;
			TcpServerBase.ClientConnected += TcpServerBase_ClientConnected;
			return this;
		}

		public Core PreInitTasks() {
			if (File.Exists(Constants.TraceLogPath)) {
				File.Delete(Constants.TraceLogPath);
			}

			Helpers.SetFileSeperator();
			Helpers.CheckMultipleProcess();
			IsNetworkAvailable = Helpers.CheckForInternetConnection();

			if (!IsNetworkAvailable) {
				Logger.Log("No Internet connection.", Enums.LogLevels.Warn);
				Logger.Log($"Starting {AssistantName} in offline mode...");
			}

			return this;
		}

		public Core LoadConfiguration() {
			Config = Config.LoadConfig();
			return this;
		}

		public Core VariableAssignation() {
			StartupTime = DateTime.Now;
			AssistantName = Config.AssistantDisplayName;
			Logger.LogIdentifier = AssistantName;
			RunningPlatform = Helpers.GetOsPlatform();
			Config.ProgramLastStartup = StartupTime;
			Constants.LocalIP = Helpers.GetLocalIpAddress();
			Constants.ExternelIP = Helpers.GetExternalIp() ?? "-Invalid-";
			return this;
		}

		public Core StartTcpServer(int port, int backlog) {
			_ = TcpServerBase.Start(port, backlog).Result;

			return this;
		}

		public Core StartTcpServer() {
			_ = TcpServerBase.Start(5555, 15).Result;

			return this;
		}

		public Core VerifyEnvironment() {
			if (!Helpers.IsRaspberryEnvironment()) {
				DisablePiMethods = true;
				IsUnknownOs = true;
			}
			else {
				SendLocalIp(!Helpers.IsNullOrEmpty(Constants.LocalIP));
			}

			return this;
		}

		public Core StartConsoleTitleUpdater() {
			Helpers.InBackgroundThread(SetConsoleTitle, "Console Title Updater", true);
			return this;
		}

		public Core DisplayASCIILogo() {
			Helpers.GenerateAsciiFromText(Config.AssistantDisplayName);
			return this;
		}

		public Core DisplayASCIILogo(string text) {
			if (!string.IsNullOrEmpty(text)) {
				Helpers.GenerateAsciiFromText(text);
			}
			return this;
		}

		public Core Misc() {
			File.WriteAllText("version.txt", Constants.Version?.ToString());
			Logger.Log($"X---------------- Starting {AssistantName} v{Constants.Version} ----------------X", Enums.LogLevels.Ascii);

			return this;
		}

		public Core StartConfigWatcher() {
			ConfigWatcher.InitConfigWatcher();
			return this;
		}

		public Core StartPushBulletService() {
			if (!string.IsNullOrEmpty(Config.PushBulletApiKey)) {
				Helpers.InBackground(() => {
					if (PushBulletService.InitPushBulletService(Config.PushBulletApiKey).InitPushService()) {
						Logger.Log("Push bullet service started.", Enums.LogLevels.Trace);
					}
				});
			}

			return this;
		}

		public Core CheckAndUpdate() {
			Helpers.InBackground(async () => await Update.CheckAndUpdateAsync(true).ConfigureAwait(false));
			return this;
		}

		public Core StartKestrel() {
			if (Config.KestrelServer) {
				Helpers.InBackground(async () => await KestrelServer.Start().ConfigureAwait(false));
			}

			return this;
		}

		public Core StartModules() {
			if (Config.EnableModules) {
				Helpers.InBackground(async () => {
					if (await ModuleLoader.LoadAsync().ConfigureAwait(false)) {
						await ModuleLoader.InitServiceAsync().ConfigureAwait(false);
						ModuleWatcher.InitModuleWatcher();
					}
				});
			}

			return this;
		}

		public Core StartPinController() {
			PiController?.InitController(Enums.EGpioDriver.RaspberryIODriver);

			if (PiController != null && !PiController.IsControllerProperlyInitialized) {
				PiController = null;
			}

			return this;
		}

		public Core MarkInitializationCompletion() {
			CoreInitiationCompleted = true;
			return this;
		}

		private static void TcpServerBase_ClientConnected(object sender, OnClientConnectedEventArgs e) {
			lock (ClientManagers) {
				ClientManagers.TryAdd(e.ClientUid, new TcpServerClientManager(e.ClientUid));
			}
		}

		private static void TcpServerBase_ServerStarted(object sender, OnServerStartedListerningEventArgs e) => Logger.Log($"TCP Server listening at {e.ListerningAddress} / {e.ServerPort}");

		private static void TcpServerBase_ServerShutdown(object sender, OnServerShutdownEventArgs e) => Logger.Log($"TCP shutting down.");

		private static void EventLogger_LogMessageReceived(object sender, LogMessageEventArgs e) {
			switch (e.LogLevel) {
				case LogEnums.LogLevel.DEBUG:
					Logger.Log(e.LogMessage, Enums.LogLevels.Debug, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.TRACE:
					Logger.Log(e.LogMessage, Enums.LogLevels.Debug, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.INFO:
					Logger.Log(e.LogMessage, Enums.LogLevels.Info, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.WARN:
					Logger.Log(e.LogMessage, Enums.LogLevels.Warn, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.ERROR:
					Logger.Log(e.LogMessage, Enums.LogLevels.Error, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.EXCEPTION:
					Logger.Log(e.LogMessage, Enums.LogLevels.Error, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.FATAL:
					Logger.Log(e.LogMessage, Enums.LogLevels.Fatal, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				case LogEnums.LogLevel.CUSTOM:
					Logger.Log(e.LogMessage, Enums.LogLevels.Custom, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					return;
				default:
					goto case LogEnums.LogLevel.INFO;
			}
		}
		
		private static void SetConsoleTitle() {
			Helpers.SetConsoleTitle($"{Helpers.TimeRan()} | http://{Constants.LocalIP}:9090/ | {DateTime.Now.ToLongTimeString()} | Uptime: {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 3)} minutes");

			if (RefreshConsoleTitleTimer == null) {
				RefreshConsoleTitleTimer = new Timer(e => SetConsoleTitle(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
			}
		}

		private static async Task DisplayConsoleCommandMenu() {
			Logger.Log("Displaying console command window", Enums.LogLevels.Trace);
			Logger.Log($"------------------------- COMMAND WINDOW -------------------------", Enums.LogLevels.UserInput);
			Logger.Log($"{Constants.ConsoleShutdownKey} - Shutdown assistant.", Enums.LogLevels.UserInput);

			if (!DisablePiMethods) {
				Logger.Log($"{Constants.ConsoleRelayCommandMenuKey} - Display relay pin control menu.", Enums.LogLevels.UserInput);
				Logger.Log($"{Constants.ConsoleRelayCycleMenuKey} - Display relay cycle control menu.", Enums.LogLevels.UserInput);

				if (Config.EnableModules) {
					Logger.Log($"{Constants.ConsoleModuleShutdownKey} - Invoke shutdown method on all currently running modules.", Enums.LogLevels.UserInput);
				}

				Logger.Log($"{Constants.ConsoleMorseCodeKey} - Morse code generator for the specified text.", Enums.LogLevels.UserInput);
			}

			Logger.Log($"{Constants.ConsoleTestMethodExecutionKey} - Run preconfigured test methods or tasks.", Enums.LogLevels.UserInput);
			if (WeatherApi != null) {
				Logger.Log($"{Constants.ConsoleWeatherInfoKey} - Get weather info of the specified location based on the pin code.", Enums.LogLevels.UserInput);
			}

			Logger.Log($"-------------------------------------------------------------------", Enums.LogLevels.UserInput);
			Logger.Log("Awaiting user input: \n", Enums.LogLevels.UserInput);

			int failedTriesCount = 0;
			int maxTries = 3;

			while (true) {
				if (failedTriesCount > maxTries) {
					Logger.Log($"Multiple wrong inputs. please start the command menu again  by pressing {Constants.ConsoleCommandMenuKey} key.", Enums.LogLevels.Warn);
					return;
				}

				char pressedKey = Console.ReadKey().KeyChar;

				switch (pressedKey) {
					case Constants.ConsoleShutdownKey:
						Logger.Log("Shutting down assistant...", Enums.LogLevels.Warn);
						await Task.Delay(1000).ConfigureAwait(false);
						await Exit(0).ConfigureAwait(false);
						return;

					case Constants.ConsoleRelayCommandMenuKey when !DisablePiMethods:
						Logger.Log("Displaying relay command menu...", Enums.LogLevels.Warn);
						DisplayRelayCommandMenu();
						return;

					case Constants.ConsoleRelayCycleMenuKey when !DisablePiMethods:
						Logger.Log("Displaying relay cycle menu...", Enums.LogLevels.Warn);
						await DisplayRelayCycleMenu().ConfigureAwait(false);
						return;

					case Constants.ConsoleRelayCommandMenuKey when DisablePiMethods:
					case Constants.ConsoleRelayCycleMenuKey when DisablePiMethods:
						Logger.Log("Assistant is running in an Operating system/Device which doesn't support GPIO pin controlling functionality.", Enums.LogLevels.Warn);
						return;

					case Constants.ConsoleMorseCodeKey when !DisablePiMethods:
						Logger.Log("Enter text to convert to Morse: ");
						string morseCycle = Console.ReadLine();
						if (PiController == null) {
							return;
						}

						if (PiController.GetMorseTranslator().IsTranslatorOnline) {
							await PiController.GetMorseTranslator().RelayMorseCycle(morseCycle, Config.OutputModePins[0]).ConfigureAwait(false);
						}
						else {
							Logger.Log("Could not convert due to an unknown error.", Enums.LogLevels.Warn);
						}
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
								Logger.Log("Please try again!", Enums.LogLevels.Warn);
								continue;
							}
						}

						if (Config.OpenWeatherApiKey != null && !Config.OpenWeatherApiKey.IsNull() && WeatherApi != null) {
							(bool status, WeatherData response) = WeatherApi.GetWeatherInfo(Config.OpenWeatherApiKey, pinCode, "in");

							if (status) {
								Logger.Log($"------------ Weather information for {pinCode}/{response.LocationName} ------------", Enums.LogLevels.Success);
								Logger.Log($"Temperature: {response.Temperature}", Enums.LogLevels.Success);
								Logger.Log($"Humidity: {response.Temperature}", Enums.LogLevels.Success);
								Logger.Log($"Latitude: {response.Latitude}", Enums.LogLevels.Success);
								Logger.Log($"Longitude: {response.Logitude}", Enums.LogLevels.Success);
								Logger.Log($"Location name: {response.LocationName}", Enums.LogLevels.Success);
								Logger.Log($"Pressure: {response.Pressure}", Enums.LogLevels.Success);
								Logger.Log($"Wind speed: {response.WindDegree}", Enums.LogLevels.Success);
							}
							else {
								Logger.Log("Failed to fetch weather information, try again later!");
							}
						}

						return;

					case Constants.ConsoleTestMethodExecutionKey:
						Logger.Log("Executing test methods/tasks", Enums.LogLevels.Warn);
						Logger.Log("Test method execution finished successfully!", Enums.LogLevels.Success);
						return;

					case Constants.ConsoleModuleShutdownKey when ModuleLoader.Modules.Count > 0 && Config.EnableModules:
						Logger.Log("Shutting down all modules...", Enums.LogLevels.Warn);
						ModuleLoader.OnCoreShutdown();
						return;

					case Constants.ConsoleModuleShutdownKey when ModuleLoader.Modules.Count <= 0:
						Logger.Log("There are no modules to shutdown...");
						return;

					default:
						if (failedTriesCount > maxTries) {
							Logger.Log($"Unknown key was pressed. ({maxTries - failedTriesCount} tries left)", Enums.LogLevels.Warn);
						}

						failedTriesCount++;
						continue;
				}
			}
		}

		private static async Task KeepAlive() {
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", Enums.LogLevels.Success);

			while (!KeepAliveToken.Token.IsCancellationRequested) {
				char pressedKey = Console.ReadKey().KeyChar;

				switch (pressedKey) {
					case Constants.ConsoleCommandMenuKey:
						await DisplayConsoleCommandMenu().ConfigureAwait(false);
						break;

					default:
						Logger.Log("Unknown key pressed during KeepAlive() command", Enums.LogLevels.Trace);
						continue;
				}
			}
		}

		private static void ParseStartupArguments(string[] args) {
			if (!args.Any() || args == null) {
				return;
			}

			Parser.Default.ParseArguments<Options>(args).WithParsed(x => {
				if (x.Debug) {
					Logger.Log("Debug mode enabled. Logging trace data to console.", Enums.LogLevels.Warn);
					Config.Debug = true;
				}

				if (x.Safe) {
					Logger.Log("Safe mode enabled. Only preconfigured gpio pins are allowed to be modified.", Enums.LogLevels.Warn);
					Config.GpioSafeMode = true;
				}

				if (x.EnableFirstChance) {
					Logger.Log("First chance exception logging is enabled.", Enums.LogLevels.Warn);
					Config.EnableFirstChanceLog = true;
				}

				if (x.TextToSpeech) {
					Logger.Log("Enabled text to speech service via startup arguments.", Enums.LogLevels.Warn);
					Config.EnableTextToSpeech = true;
				}

				if (x.DisableFirstChance) {
					Logger.Log("Disabling first chance exception logging with debug mode.", Enums.LogLevels.Warn);
					DisableFirstChanceLogWithDebug = true;
				}
			});
		}

		private static void DisplayRelayCommandMenu() {
			Logger.Log("-------------------- RELAY COMMAND MENU --------------------", Enums.LogLevels.UserInput);
			Logger.Log("1 | Relay pin 1", Enums.LogLevels.UserInput);
			Logger.Log("2 | Relay pin 2", Enums.LogLevels.UserInput);
			Logger.Log("3 | Relay pin 3", Enums.LogLevels.UserInput);
			Logger.Log("4 | Relay pin 4", Enums.LogLevels.UserInput);
			Logger.Log("5 | Relay pin 5", Enums.LogLevels.UserInput);
			Logger.Log("6 | Relay pin 6", Enums.LogLevels.UserInput);
			Logger.Log("7 | Relay pin 7", Enums.LogLevels.UserInput);
			Logger.Log("8 | Relay pin 8", Enums.LogLevels.UserInput);
			Logger.Log("9 | Schedule task for specified relay pin", Enums.LogLevels.UserInput);
			Logger.Log("0 | Exit menu", Enums.LogLevels.UserInput);
			Logger.Log("Press any key (between 0 - 9) for their respective option.\n", Enums.LogLevels.UserInput);
			ConsoleKeyInfo key = Console.ReadKey();
			Logger.Log("\n", Enums.LogLevels.UserInput);

			if (!int.TryParse(key.KeyChar.ToString(), out int SelectedValue)) {
				Logger.Log("Could not parse the input key. please try again!", Enums.LogLevels.Error);
				Logger.Log("Command menu closed.");
				Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", Enums.LogLevels.Success);
				return;
			}

			static void set(int pin) {
				if (PiController == null) {
					return;
				}

				GpioPinConfig pinStatus = PiController.GetPinController().GetGpioConfig(pin);
				if (pinStatus.IsPinOn) {
					PiController.GetPinController().SetGpioValue(pin, Enums.GpioPinMode.Output, Enums.GpioPinState.Off);
					Logger.Log($"Successfully set {pin} pin to OFF.", Enums.LogLevels.Success);
				}
				else {
					PiController.GetPinController().SetGpioValue(pin, Enums.GpioPinMode.Output, Enums.GpioPinState.On);
					Logger.Log($"Successfully set {pin} pin to ON.", Enums.LogLevels.Success);
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
					Logger.Log("Please enter the pin u want to configure: ", Enums.LogLevels.UserInput);
					string pinNumberKey = Console.ReadLine();

					if (!int.TryParse(pinNumberKey, out int pinNumber) || Convert.ToInt32(pinNumberKey) <= 0) {
						Logger.Log("Your entered pin number is incorrect. please enter again.", Enums.LogLevels.UserInput);

						pinNumberKey = Console.ReadLine();
						if (!int.TryParse(pinNumberKey, out pinNumber) || Convert.ToInt32(pinNumberKey) <= 0) {
							Logger.Log("Your entered pin number is incorrect again. press m for menu, and start again!", Enums.LogLevels.UserInput);
							return;
						}
					}

					Logger.Log("Please enter the amount of delay you want in between the task. (in minutes)", Enums.LogLevels.UserInput);
					string delayInMinuteskey = Console.ReadLine();
					if (!int.TryParse(delayInMinuteskey, out int delay) || Convert.ToInt32(delayInMinuteskey) <= 0) {
						Logger.Log("Your entered delay is incorrect. please enter again.", Enums.LogLevels.UserInput);

						delayInMinuteskey = Console.ReadLine();
						if (!int.TryParse(delayInMinuteskey, out delay) || Convert.ToInt32(delayInMinuteskey) <= 0) {
							Logger.Log("Your entered pin is incorrect again. press m for menu, and start again!", Enums.LogLevels.UserInput);
							return;
						}
					}

					Logger.Log("Please enter the status u want the task to configure: (0 = OFF, 1 = ON)", Enums.LogLevels.UserInput);

					string pinStatuskey = Console.ReadLine();
					if (!int.TryParse(pinStatuskey, out int pinStatus) || (Convert.ToInt32(pinStatuskey) != 0 && Convert.ToInt32(pinStatus) != 1)) {
						Logger.Log("Your entered pin status is incorrect. please enter again.", Enums.LogLevels.UserInput);

						pinStatuskey = Console.ReadLine();
						if (!int.TryParse(pinStatuskey, out pinStatus) || (Convert.ToInt32(pinStatuskey) != 0 && Convert.ToInt32(pinStatus) != 1)) {
							Logger.Log("Your entered pin status is incorrect again. press m for menu, and start again!", Enums.LogLevels.UserInput);
							return;
						}
					}

					if (PiController == null) {
						return;
					}

					GpioPinConfig status = PiController.GetPinController().GetGpioConfig(pinNumber);

					if (status.IsPinOn && pinStatus.Equals(1)) {
						Logger.Log("Pin is already configured to be in ON State. Command doesn't make any sense.");
						return;
					}

					if (!status.IsPinOn && pinStatus.Equals(0)) {
						Logger.Log("Pin is already configured to be in OFF State. Command doesn't make any sense.");
						return;
					}

					if (Config.IRSensorPins.Count() > 0 && Config.IRSensorPins.Contains(pinNumber)) {
						Logger.Log("Sorry, the specified pin is preconfigured for IR Sensor. cannot modify!");
						return;
					}

					if (!Config.RelayPins.Contains(pinNumber)) {
						Logger.Log("Sorry, the specified pin doesn't exist in the relay pin category.");
						return;
					}

					Helpers.ScheduleTask(() => {
						if (status.IsPinOn && pinStatus.Equals(0)) {
							PiController.GetPinController().SetGpioValue(pinNumber, Enums.GpioPinMode.Output, Enums.GpioPinState.Off);
							Logger.Log($"Successfully finished execution of the task: {pinNumber} pin set to OFF.", Enums.LogLevels.Success);
						}

						if (!status.IsPinOn && pinStatus.Equals(1)) {
							PiController.GetPinController().SetGpioValue(pinNumber, Enums.GpioPinMode.Output, Enums.GpioPinState.On);
							Logger.Log($"Successfully finished execution of the task: {pinNumber} pin set to ON.", Enums.LogLevels.Success);
						}
					}, TimeSpan.FromMinutes(delay));

					Logger.Log(
						pinStatus.Equals(0)
							? $"Successfully scheduled a task: set {pinNumber} pin to OFF"
							: $"Successfully scheduled a task: set {pinNumber} pin to ON", Enums.LogLevels.Success);
					break;
			}

			Logger.Log("Command menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", Enums.LogLevels.Success);
		}

		private static async Task DisplayRelayCycleMenu() {
			if (DisablePiMethods) {
				Logger.Log("You are running on incorrect OS or device. Pi controls are disabled.", Enums.LogLevels.Error);
				return;
			}

			Logger.Log("--------------------MODE MENU--------------------", Enums.LogLevels.UserInput);
			Logger.Log("1 | Relay Cycle", Enums.LogLevels.UserInput);
			Logger.Log("2 | Relay OneMany", Enums.LogLevels.UserInput);
			Logger.Log("3 | Relay OneOne", Enums.LogLevels.UserInput);
			Logger.Log("4 | Relay OneTwo", Enums.LogLevels.UserInput);
			Logger.Log("5 | Relay Single", Enums.LogLevels.UserInput);
			Logger.Log("6 | Relay Default", Enums.LogLevels.UserInput);
			Logger.Log("0 | Exit", Enums.LogLevels.UserInput);
			Logger.Log("Press any key (between 0 - 6) for their respective option.\n", Enums.LogLevels.UserInput);
			ConsoleKeyInfo key = Console.ReadKey();
			Logger.Log("\n", Enums.LogLevels.UserInput);

			if (!int.TryParse(key.KeyChar.ToString(), out int SelectedValue)) {
				Logger.Log("Could not parse the input key. please try again!", Enums.LogLevels.Error);
				Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for command menu.", Enums.LogLevels.Info);
				return;
			}

			if (Core.PiController == null) {
				return;
			}

			bool Configured;
			switch (SelectedValue) {
				case 1:
					Configured = await PiController.GetPinController().RelayTestServiceAsync(Enums.GpioCycles.Cycle).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}

					break;

				case 2:
					Configured = await PiController.GetPinController().RelayTestServiceAsync(Enums.GpioCycles.OneMany).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}

					break;

				case 3:
					Configured = await PiController.GetPinController().RelayTestServiceAsync(Enums.GpioCycles.OneOne).ConfigureAwait(false);
					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 4:
					Configured = await PiController.GetPinController().RelayTestServiceAsync(Enums.GpioCycles.OneTwo).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 5:
					Logger.Log("\nPlease select the channel (3, 4, 17, 2, 27, 10, 22, 9, etc): ", Enums.LogLevels.UserInput);
					string singleKey = Console.ReadLine();

					if (!int.TryParse(singleKey, out int selectedsingleKey)) {
						Logger.Log("Could not parse the input key. please try again!", Enums.LogLevels.Error);
						goto case 5;
					}

					Configured = await PiController.GetPinController().RelayTestServiceAsync(Enums.GpioCycles.Single, selectedsingleKey).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 6:
					Configured = await PiController.GetPinController().RelayTestServiceAsync(Enums.GpioCycles.Default).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 0:
					Logger.Log("Exiting from menu...", Enums.LogLevels.UserInput);
					return;

				default:
					goto case 0;
			}

			Logger.Log(Configured ? "Test successful!" : "Test Failed!");

			Logger.Log("Relay menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} to display command menu.");
		}

		public static async Task OnNetworkDisconnected() {
			try {
				await NetworkSemaphore.WaitAsync().ConfigureAwait(false);
				IsNetworkAvailable = false;
				await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.NetworkDisconnected).ConfigureAwait(false);
				Constants.ExternelIP = "Internet connection lost.";

				if (Update != null) {
					Update.StopUpdateTimer();
					Logger.Log("Stopped update timer.", Enums.LogLevels.Warn);
				}
			}
			finally {
				NetworkSemaphore.Release();
			}
		}

		public static async Task OnNetworkReconnected() {
			try {
				await NetworkSemaphore.WaitAsync().ConfigureAwait(false);
				IsNetworkAvailable = true;
				await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.NetworkReconnected).ConfigureAwait(false);
				Constants.ExternelIP = Task.Run(Helpers.GetExternalIp).Result;

				if (Config.AutoUpdates && IsNetworkAvailable) {
					Logger.Log("Checking for any new version...", Enums.LogLevels.Trace);
					File.WriteAllText("version.txt", Constants.Version?.ToString());
					await Update.CheckAndUpdateAsync(true).ConfigureAwait(false);
				}
			}
			finally {
				NetworkSemaphore.Release();
			}
		}

		public static async Task OnExit() {
			Logger.Log("Shutting down...");

			if (ModuleLoader != null) {
				await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.AssistantShutdown).ConfigureAwait(false);
			}

			PiController?.InitGpioShutdownTasks();
			TaskManager?.OnCoreShutdownRequested();
			Update?.StopUpdateTimer();
			RefreshConsoleTitleTimer?.Dispose();
			ConfigWatcher?.StopConfigWatcher();
			ModuleWatcher?.StopModuleWatcher();

			if (TcpServerBase.IsServerListerning) {
				await TcpServerBase.Shutdown().ConfigureAwait(false);
			}

			if (KestrelServer.IsServerOnline) {
				await KestrelServer.Stop().ConfigureAwait(false);
			}

			ModuleLoader?.OnCoreShutdown();
			Config.ProgramLastShutdown = DateTime.Now;
			Config.SaveConfig(Config);
			Logger.Log("Finished on exit tasks.", Enums.LogLevels.Trace);
		}

		public static async Task Exit(int exitCode = 0) {
			if (exitCode != 0) {
				Logger.Log("Exiting with nonzero error code...", Enums.LogLevels.Error);
			}

			if (exitCode == 0) {
				await OnExit().ConfigureAwait(false);
			}

			Logger.Log("Bye, have a good day sir!");
			Logging.LoggerOnShutdown();
			KeepAliveToken.Cancel();
			Environment.Exit(exitCode);
		}

		public static async Task Restart(int delay = 10) {
			if (!Config.AutoRestart) {
				Logger.Log("Auto restart is turned off in config.", Enums.LogLevels.Warn);
				return;
			}

			Helpers.ScheduleTask(() => "cd /home/pi/Desktop/HomeAssistant/Helpers/Restarter && dotnet RestartHelper.dll".ExecuteBash(true), TimeSpan.FromSeconds(delay));
			await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
			await Exit(0).ConfigureAwait(false);
		}

		public static async Task SystemShutdown() {
			await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.SystemShutdown).ConfigureAwait(false);
			if (Helpers.IsRaspberryEnvironment()) {
				Logger.Log($"Assistant is running on raspberry pi.", Enums.LogLevels.Trace);
				Logger.Log("Shutting down pi...", Enums.LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				await Pi.ShutdownAsync().ConfigureAwait(false);
				return;
			}

			if (Helpers.GetOsPlatform() == OSPlatform.Windows) {
				Logger.Log($"Assistant is running on a windows system.", Enums.LogLevels.Trace);
				Logger.Log("Shutting down system...", Enums.LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /t 0") {
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(psi);
			}
		}

		public static async Task SystemRestart() {
			await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.SystemRestart).ConfigureAwait(false);
			if (Helpers.IsRaspberryEnvironment()) {
				Logger.Log($"Assistant is running on raspberry pi.", Enums.LogLevels.Trace);
				Logger.Log("Restarting pi...", Enums.LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				await Pi.RestartAsync().ConfigureAwait(false);
				return;
			}

			if (Helpers.GetOsPlatform() == OSPlatform.Windows) {
				Logger.Log($"Assistant is running on a windows system.", Enums.LogLevels.Trace);
				Logger.Log("Restarting system...", Enums.LogLevels.Warn);
				await OnExit().ConfigureAwait(false);
				ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/r /t 0") {
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(psi);
			}
		}

		/// <summary>
		/// The method sends the current working local ip to an central server which i personally use for such tasks and for authentication etc.
		/// You have to specify such a server manually else contact me personally for my server IP.
		/// We use this so that the mobile controller app of the assistant can connect to the assistant running on the connected local interface.
		/// </summary>
		/// <param name="enableRecrussion">Specify if you want to execute this method in a loop every SendIpDelay minutes. (recommended)</param>
		private static void SendLocalIp(bool enableRecrussion) {
			string? localIp = Helpers.GetLocalIpAddress();

			if (localIp == null || Helpers.IsNullOrEmpty(localIp)) {
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

			Logger.Log($"{Constants.LocalIP} IP request send!", Enums.LogLevels.Trace);
			if (enableRecrussion) {
				Helpers.ScheduleTask(() => SendLocalIp(enableRecrussion), TimeSpan.FromMinutes(SendIpDelay));
			}
		}
	}
}
