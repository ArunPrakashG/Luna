using CommandLine;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules;
using HomeAssistant.Server;
using HomeAssistant.Update;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.Swan;
using Unosquare.WiringPi;
using static HomeAssistant.Core.Enums;
using Logging = HomeAssistant.Log.Logging;

namespace HomeAssistant.Core {
	public class Options {

		[Option('d', "debug", Required = false, HelpText = "Displays all Trace level messages to console. (for debugging)")]
		public bool Debug { get; set; }

		[Option('s', "safe", Required = false, HelpText = "Enables safe mode so that only pre configured pins can be modified.")]
		public bool Safe { get; set; }

		[Option('f', "firstchance", Required = false, HelpText = "Enables logging of first chance exceptions to console.")]
		public bool EnableFirstChance { get; set; }

		[Option('t', "tts", Required = false, HelpText = "Enable text to speech system for assistant.")]
		public bool TextToSpeech { get; set; }

		[Option("df", Required = false, HelpText = "Disable first chance exception logging when debug mode is enabled.")]
		public bool DisableFirstChance { get; set; }
	}

	public class Tess {
		private static readonly Logger Logger = new Logger("TESS");
		public static GPIOController Controller { get; private set; }
		public static Updater Update { get; private set; } = new Updater();
		public static ProcessStatus AssistantStatus { get; private set; }
		public static CoreConfig Config { get; set; } = new CoreConfig();
		private static ConfigWatcher ConfigWatcher { get; set; } = new ConfigWatcher();
		private static ModuleWatcher ModuleWatcher { get; set; } = new ModuleWatcher();
		public static GPIOConfigHandler GPIOConfigHandler { get; private set; } = new GPIOConfigHandler();
		private static GPIOConfigRoot GPIORootObject { get; set; } = new GPIOConfigRoot();
		private static List<GPIOPinConfig> GPIOConfig { get; set; } = new List<GPIOPinConfig>();
		public static GpioEventManager EventManager { get; set; }
		public static TaskList TaskManager { get; private set; } = new TaskList();
		public static ModuleInitializer ModuleLoader { get; private set; }
		public static DateTime StartupTime { get; private set; }
		private static Timer RefreshConsoleTitleTimer { get; set; }
		public static DynamicWatcher DynamicWatcher { get; set; } = new DynamicWatcher();
		public static bool CoreInitiationCompleted { get; private set; }
		public static bool DisablePiMethods { get; private set; }
		public static bool IsUnknownOs { get; set; }
		public static bool IsNetworkAvailable { get; set; }
		public static bool DisableFirstChanceLogWithDebug { get; set; }
		public static bool GracefullModuleShutdown { get; set; } = false;

		public static async Task<bool> InitCore(string[] args) {
			Helpers.CheckMultipleProcess();
			StartupTime = DateTime.Now;
			Helpers.SetFileSeperator();
			Logger.Log("Verifying internet connectivity...", LogLevels.Trace);

			if (Helpers.CheckForInternetConnection()) {
				Logger.Log("Internet connection verified!", LogLevels.Trace);
				IsNetworkAvailable = true;
			}
			else {
				Logger.Log("No internet connection.", LogLevels.Warn);
				Logger.Log("Starting TESS in offline mode...");
				IsNetworkAvailable = false;
			}

			try {
				await Helpers.DisplayTessASCII().ConfigureAwait(false);
				Constants.ExternelIP = Helpers.GetExternalIp();
				Constants.LocalIP = Helpers.GetLocalIpAddress();

				if (string.IsNullOrEmpty(Constants.ExternelIP) || string.IsNullOrWhiteSpace(Constants.ExternelIP)) {
					Constants.ExternelIP = "Failed. No internet connection.";
				}

				Helpers.InBackgroundThread(SetConsoleTitle, "Console Title Updater");
				Logger.Log($"X---------------- Starting TESS Assistant v{Constants.Version} ----------------X", LogLevels.Ascii);
				Logger.Log("Loading core config...", LogLevels.Trace);
				try {
					Config = Config.LoadConfig();
				}
				catch (NullReferenceException) {
					Logger.Log("Fatal error has occured during loading Core Config. exiting...", LogLevels.Error);
					await Exit(1).ConfigureAwait(false);
					return false;
				}

				ConfigWatcher.InitConfigWatcher();
				ParseStartupArguments(args);

				if (!Helpers.IsRaspberryEnvironment() || Helpers.GetOsPlatform() != OSPlatform.Linux) {
					DisablePiMethods = true;
					IsUnknownOs = true;
				}

				Logger.Log("Loading GPIO config...", LogLevels.Trace);
				try {
					GPIORootObject = GPIOConfigHandler.LoadConfig();

					if (GPIORootObject != null) {
						GPIOConfig = GPIORootObject.GPIOData;
					}
				}
				catch (NullReferenceException) {
					Logger.Log("Fatal error has occured during loading GPIO Config. exiting...", LogLevels.Error);
					await Exit(1).ConfigureAwait(false);
					return false;
				}

				if (Helpers.GetOsPlatform().Equals(OSPlatform.Windows)) {
					AssistantStatus = new ProcessStatus();
				}
				else {
					Logger.Log("Could not start performence counters as it is not supported on this platform.", LogLevels.Trace);
				}

				Config.ProgramLastStartup = StartupTime;

				bool Token = false;

				try {
					string checkForToken = Helpers.FetchVariable(0, true, "GITHUB_TOKEN");

					if (string.IsNullOrEmpty(checkForToken) || string.IsNullOrWhiteSpace(checkForToken)) {
						Logger.Log("Github token isnt found. Updates will be disabled.", LogLevels.Warn);
						Token = false;
					}
					else {
						Token = true;
					}
				}
				catch (NullReferenceException) {
					Logger.Log("Github token isnt found. Updates will be disabled.", LogLevels.Warn);
					Token = false;
				}

				if (Token && Config.AutoUpdates && IsNetworkAvailable) {
					Logger.Log("Checking for any new version...", LogLevels.Trace);
					File.WriteAllText("version.txt", Constants.Version.ToString());
					Update.CheckAndUpdate(true);
				}

				if (Config.KestrelServer) {
					if (IsNetworkAvailable) {
						await KestrelServer.Start().ConfigureAwait(false);
					}
					else {
						Logger.Log("Could not start Kestrel server as network is unavailable.", LogLevels.Warn);
					}
				}

				ModuleLoader = new ModuleInitializer();

				if (IsNetworkAvailable) {
					if (Config.EnableModules) {
						(bool, LoadedModules) loadStatus = ModuleLoader.LoadModules();
						if (!loadStatus.Item1) {
							Logger.Log("Failed to load modules.", LogLevels.Warn);
						}
						else {

						}
					}
					else {
						Logger.Log("Not starting modules as its disabled in config file.", LogLevels.Trace);
					}
				}
				else {
					Logger.Log("Could not start the modules as network is unavailable.", LogLevels.Warn);
				}

				await PostInitTasks().ConfigureAwait(false);
			}
			catch (Exception e) {
				Logger.Log(e, LogLevels.Fatal);
				return false;
			}
			return true;
		}

		private static async Task PostInitTasks() {
			Logger.Log("Running post-initiation tasks...", LogLevels.Trace);
			ModuleWatcher.InitConfigWatcher();
			if (Helpers.GetOsPlatform().Equals(OSPlatform.Windows)) {
				Controller = new GPIOController(GPIORootObject, GPIOConfig, GPIOConfigHandler);
				Logger.Log("Gpio controller has been started despite OS differences, there are chances of crashs and some methods won't work.", LogLevels.Error);
			}

			if (!DisablePiMethods) {
				if (Config.EnableGpioControl) {
					Pi.Init<BootstrapWiringPi>();
					Controller = new GPIOController(GPIORootObject, GPIOConfig, GPIOConfigHandler);
					Controller.DisplayPiInfo();
					EventManager = Controller.GpioPollingManager;
					Logger.Log("Successfully Initiated Pi Configuration!");
				}
				else {
					Logger.Log("Not starting GPIO controller as its disabled in config file.");
				}

			}
			else {
				Logger.Log("Disabled Raspberry Pi related methods and initiation tasks.");
			}

			CoreInitiationCompleted = true;

			if (Config.DisplayStartupMenu && !DisablePiMethods) {
				await DisplayRelayCycleMenu().ConfigureAwait(false);
			}

			TTSService.SpeakText("TESS Home assistant have been sucessfully started!", SpeechContext.TessStartup, true);
			await KeepAlive().ConfigureAwait(false);
		}

		private static void SetConsoleTitle() {
			Helpers.SetConsoleTitle($"{Helpers.TimeRan()} | {Constants.LocalIP}:{Config.KestrelServerPort} | {DateTime.Now.ToLongTimeString()} | Uptime: {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 3)} minutes");

			if (RefreshConsoleTitleTimer == null) {
				RefreshConsoleTitleTimer = new Timer(e => SetConsoleTitle(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
			}
		}

		private static async Task DisplayConsoleCommandMenu() {
			Logger.Log("Displaying console command window", LogLevels.Trace);
			Logger.Log($"------------------------- COMMAND WINDOW -------------------------", LogLevels.UserInput);
			Logger.Log($"{Constants.ConsoleQuickShutdownKey} - Quick shutdown the assistant.", LogLevels.UserInput);
			Logger.Log($"{Constants.ConsoleDelayedShutdownKey} - Shutdown assistant in 5 seconds.", LogLevels.UserInput);

			if (!DisablePiMethods) {
				Logger.Log($"{Constants.ConsoleRelayCommandMenuKey} - Display relay pin control menu.", LogLevels.UserInput);
				Logger.Log($"{Constants.ConsoleRelayCycleMenuKey} - Display relay cycle control menu.", LogLevels.UserInput);
			}
			
			Logger.Log($"{Constants.ConsoleTestMethodExecutionKey} - Run pre-configured test methods or tasks.", LogLevels.UserInput);

			if (Config.EnableModules) {
				Logger.Log($"{Constants.ConsoleModuleShutdownKey} - Invoke shutdown method on all currently running modules.", LogLevels.UserInput);
			}
			
			Logger.Log($"-------------------------------------------------------------------", LogLevels.UserInput);
			Logger.Log("Awaiting user input: \n", LogLevels.UserInput);

			int failedTriesCount = 0;
			int maxTries = 3;

			while (true) {
				if (failedTriesCount > maxTries) {
					Logger.Log($"Multiple wrong inputs. please start the command menu again  by pressing {Constants.ConsoleCommandMenuKey} key.", LogLevels.Warn);
					return;
				}

				char pressedKey = Console.ReadKey().KeyChar;

				switch (pressedKey) {
					case Constants.ConsoleQuickShutdownKey: {
							Logger.Log("Force quitting assistant...", LogLevels.Warn);
							await Exit(true).ConfigureAwait(false);
						}
						return;

					case Constants.ConsoleDelayedShutdownKey: {
							Logger.Log("Gracefully shutting down assistant...", LogLevels.Warn);
							GracefullModuleShutdown = true;
							await Task.Delay(5000).ConfigureAwait(false);
							await Exit(0).ConfigureAwait(false);
						}
						return;

					case Constants.ConsoleRelayCommandMenuKey when !DisablePiMethods: {
							Logger.Log("Displaying relay command menu...", LogLevels.Warn);
							DisplayRelayCommandMenu();
						}
						return;

					case Constants.ConsoleRelayCycleMenuKey when !DisablePiMethods: {
							Logger.Log("Displaying relay cycle menu...", LogLevels.Warn);
							await DisplayRelayCycleMenu().ConfigureAwait(false);
						}
						return;

					case Constants.ConsoleRelayCommandMenuKey when DisablePiMethods: {
							Logger.Log("Assistant is running in an Operating system/Device which doesnt support GPIO pin controlling functionality.", LogLevels.Warn);

						}
						return;

					case Constants.ConsoleRelayCycleMenuKey when DisablePiMethods: {
							Logger.Log("Assistant is running in an Operating system/Device which doesnt support GPIO pin controlling functionality.", LogLevels.Warn);

						}
						return;

					case Constants.ConsoleTestMethodExecutionKey: {
							Logger.Log("Executing test methods/tasks", LogLevels.Warn);
							Logger.Log("Test method execution finished successfully!", LogLevels.Sucess);
						}
						return;

					case Constants.ConsoleModuleShutdownKey when !ModuleLoader.LoadedModules.IsModulesEmpty && Config.EnableModules: {
							Logger.Log("Shutting down all modules...", LogLevels.Warn);
							await ModuleLoader.OnCoreShutdown().ConfigureAwait(false);
						}
						return;
					case Constants.ConsoleModuleShutdownKey when ModuleLoader.LoadedModules.IsModulesEmpty: {
							Logger.Log("There are no modules to shutdown...");
						}
						return;

					default: {
							if (failedTriesCount > maxTries) {
								Logger.Log($"Unknown key was pressed. ({maxTries - failedTriesCount} tries left)", LogLevels.Warn);
							}

							failedTriesCount++;
							continue;
						}
				}
			}
		}

		private static async Task KeepAlive() {
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", LogLevels.Sucess);

			while (true) {
				char pressedKey = Console.ReadKey().KeyChar;

				switch (pressedKey) {
					case Constants.ConsoleCommandMenuKey:
						await DisplayConsoleCommandMenu().ConfigureAwait(false);
						break;

					default:
						Logger.Log("Unknown key pressed during KeepAlive() command", LogLevels.Trace);
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
					Logger.Log("Debug mode enabled. Logging trace data to console.", LogLevels.Warn);
					Config.Debug = true;
				}

				if (x.Safe) {
					Logger.Log("Safe mode enabled. Only pre-configured gpio pins are allowed to be modified.", LogLevels.Warn);
					Config.GPIOSafeMode = true;
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
			Logger.Log("-------------------- RELAY COMMAND MENU --------------------", LogLevels.UserInput);
			Logger.Log("1 | Relay pin 1", LogLevels.UserInput);
			Logger.Log("2 | Relay pin 2", LogLevels.UserInput);
			Logger.Log("3 | Relay pin 3", LogLevels.UserInput);
			Logger.Log("4 | Relay pin 4", LogLevels.UserInput);
			Logger.Log("5 | Relay pin 5", LogLevels.UserInput);
			Logger.Log("6 | Relay pin 6", LogLevels.UserInput);
			Logger.Log("7 | Relay pin 7", LogLevels.UserInput);
			Logger.Log("8 | Relay pin 8", LogLevels.UserInput);
			Logger.Log("9 | Schedule task for specified relay pin", LogLevels.UserInput);
			Logger.Log("0 | Exit menu", LogLevels.UserInput);
			Logger.Log("Press any key (between 0 - 9) for their respective option.\n", LogLevels.UserInput);
			ConsoleKeyInfo key = Console.ReadKey();
			Logger.Log("\n", LogLevels.UserInput);
			if (!int.TryParse(key.KeyChar.ToString(), out int SelectedValue)) {
				Logger.Log("Could not parse the input key. please try again!", LogLevels.Error);
				Logger.Log("Command menu closed.");
				Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", LogLevels.Sucess);
				return;
			}

			GPIOPinConfig PinStatus;
			switch (SelectedValue) {
				case 1:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[0]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[0]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[0]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 2:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[1]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[1]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[1]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 3:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[2]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[2]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[2]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 4:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[3]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[3]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[3]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 5:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[4]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[4]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[4]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 6:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[5]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[5]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[5]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 7:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[6]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[6]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[6]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 8:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[7]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[7]} pin to OFF.", LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[7]} pin to ON.", LogLevels.Sucess);
					}
					break;

				case 9:
					Logger.Log("Please enter the pin u want to configure: ", LogLevels.UserInput);
					string pinNumberKey = Console.ReadLine();

					if (!int.TryParse(pinNumberKey, out int pinNumber) || Convert.ToInt32(pinNumberKey) <= 0) {
						Logger.Log("Your entered pin number is incorrect. please enter again.", LogLevels.UserInput);

						pinNumberKey = Console.ReadLine();
						if (!int.TryParse(pinNumberKey, out pinNumber) || Convert.ToInt32(pinNumberKey) <= 0) {
							Logger.Log("Your entered pin number is incorrect again. press m for menu, and start again!", LogLevels.UserInput);
							return;
						}
					}

					Logger.Log("Please enter the amount of delay you want in between the task. (in minutes)", LogLevels.UserInput);
					string delayInMinuteskey = Console.ReadLine();
					if (!int.TryParse(delayInMinuteskey, out int delay) || Convert.ToInt32(delayInMinuteskey) <= 0) {
						Logger.Log("Your entered delay is incorrect. please enter again.", LogLevels.UserInput);

						delayInMinuteskey = Console.ReadLine();
						if (!int.TryParse(delayInMinuteskey, out delay) || Convert.ToInt32(delayInMinuteskey) <= 0) {
							Logger.Log("Your entered pin is incorrect again. press m for menu, and start again!", LogLevels.UserInput);
							return;
						}
					}

					Logger.Log("Please enter the status u want the task to configure: (0 = OFF, 1 = ON)", LogLevels.UserInput);

					string pinStatuskey = Console.ReadLine();
					if (!int.TryParse(pinStatuskey, out int pinStatus) || (Convert.ToInt32(pinStatuskey) != 0 && Convert.ToInt32(pinStatus) != 1)) {
						Logger.Log("Your entered pin status is incorrect. please enter again.", LogLevels.UserInput);

						pinStatuskey = Console.ReadLine();
						if (!int.TryParse(pinStatuskey, out pinStatus) || (Convert.ToInt32(pinStatuskey) != 0 && Convert.ToInt32(pinStatus) != 1)) {
							Logger.Log("Your entered pin status is incorrect again. press m for menu, and start again!", LogLevels.UserInput);
							return;
						}
					}

					GPIOPinConfig Status = Controller.FetchPinStatus(pinNumber);

					if (Status.IsOn && pinStatus.Equals(1)) {
						Logger.Log("Pin is already configured to be in ON State. Command doesn't make any sense.");
						return;
					}

					if (!Status.IsOn && pinStatus.Equals(0)) {
						Logger.Log("Pin is already configured to be in OFF State. Command doesn't make any sense.");
						return;
					}

					if (Config.IRSensorPins.Contains(pinNumber)) {
						Logger.Log("Sorry, the specified pin is pre-configured for IR Sensor. cannot modify!");
						return;
					}

					if (!Config.RelayPins.Contains(pinNumber)) {
						Logger.Log("Sorry, the specified pin doesn't exist in the relay pin catagory.");
						return;
					}

					Helpers.ScheduleTask(() => {
						if (Status.IsOn && pinStatus.Equals(0)) {
							Controller.SetGPIO(pinNumber, GpioPinDriveMode.Output, GpioPinValue.High);
							Logger.Log($"Sucessfully finished execution of the task: {pinNumber} pin set to OFF.", LogLevels.Sucess);
						}

						if (!Status.IsOn && pinStatus.Equals(1)) {
							Controller.SetGPIO(pinNumber, GpioPinDriveMode.Output, GpioPinValue.Low);
							Logger.Log($"Sucessfully finished execution of the task: {pinNumber} pin set to ON.", LogLevels.Sucess);
						}
					}, TimeSpan.FromMinutes(delay));

					Logger.Log(
						pinStatus.Equals(0)
							? $"Successfully scheduled a task: set {pinNumber} pin to OFF"
							: $"Successfully scheduled a task: set {pinNumber} pin to ON", LogLevels.Sucess);
					break;
			}

			Logger.Log("Command menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", LogLevels.Sucess);
		}

		private static async Task DisplayRelayCycleMenu() {
			if (DisablePiMethods) {
				Logger.Log("You are running on incorrect OS or device. Pi controls are disabled.", LogLevels.Error);
				return;
			}

			Logger.Log("--------------------MODE MENU--------------------", LogLevels.UserInput);
			Logger.Log("1 | Relay Cycle", LogLevels.UserInput);
			Logger.Log("2 | Relay OneMany", LogLevels.UserInput);
			Logger.Log("3 | Relay OneOne", LogLevels.UserInput);
			Logger.Log("4 | Relay OneTwo", LogLevels.UserInput);
			Logger.Log("5 | Relay Single", LogLevels.UserInput);
			Logger.Log("6 | Relay Default", LogLevels.UserInput);
			Logger.Log("0 | Exit", LogLevels.UserInput);
			Logger.Log("Press any key (between 0 - 6) for their respective option.\n", LogLevels.UserInput);
			ConsoleKeyInfo key = Console.ReadKey();
			Logger.Log("\n", LogLevels.UserInput);

			if (!int.TryParse(key.KeyChar.ToString(), out int SelectedValue)) {
				Logger.Log("Could not parse the input key. please try again!", LogLevels.Error);
				Logger.Log("Press m for menu.", LogLevels.Info);
				return;
			}

			bool Configured;
			switch (SelectedValue) {
				case 1:
					Configured = await Controller.RelayTestService(GPIOCycles.Cycle).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}

					break;

				case 2:
					Configured = await Controller.RelayTestService(GPIOCycles.OneMany).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}

					break;

				case 3:
					Configured = await Controller.RelayTestService(GPIOCycles.OneOne).ConfigureAwait(false);
					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 4:
					Configured = await Controller.RelayTestService(GPIOCycles.OneTwo).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 5:
					Logger.Log("\nPlease select the channel (2, 3, 4, 17, 27, 22, 10, 9, etc): ", LogLevels.UserInput);
					ConsoleKeyInfo singleKey = Console.ReadKey();

					if (!int.TryParse(singleKey.KeyChar.ToString(), out int selectedsingleKey)) {
						Logger.Log("Could not prase the input key. please try again!", LogLevels.Error);
						goto case 5;
					}

					Configured = await Controller.RelayTestService(GPIOCycles.Single, selectedsingleKey).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 6:
					Configured = await Controller.RelayTestService(GPIOCycles.Default).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					break;

				case 0:
					Logger.Log("Exiting from menu...", LogLevels.UserInput);
					return;

				default:
					goto case 0;
			}

			Logger.Log(Configured ? "Test sucessfull!" : "Test Failed!");

			Logger.Log("Relay menu closed.");
			Logger.Log($"Press q to quit in 5 seconds.");
			Logger.Log($"Press e to exit application immediately.");
			Logger.Log($"Press m to display GPIO menu.");
			Logger.Log($"Press i to stop IMAP Idle notifications.");
			Logger.Log($"Press c to display command menu.");
		}

		public static async Task OnNetworkDisconnected() {
			IsNetworkAvailable = false;
			Constants.ExternelIP = "Internet connection lost.";

			if (Update != null) {
				Update.StopUpdateTimer();
				Logger.Log("Stopped update timer.", LogLevels.Warn);
			}

			if (ModuleLoader != null) {
				_ = ModuleLoader.OnCoreShutdown();
			}

			if (KestrelServer.IsServerOnline) {
				await KestrelServer.Stop().ConfigureAwait(false);
			}
		}

		public static async Task OnNetworkReconnected() {
			IsNetworkAvailable = true;
			Constants.ExternelIP = Task.Run(Helpers.GetExternalIp).Result;

			if (Config.AutoUpdates && IsNetworkAvailable) {
				Logger.Log("Checking for any new version...", LogLevels.Trace);
				File.WriteAllText("version.txt", Constants.Version.ToString());
				Update.CheckAndUpdate(true);
			}

			if (!KestrelServer.IsServerOnline) {
				await KestrelServer.Start().ConfigureAwait(false);
			}

			if (IsNetworkAvailable && ModuleLoader != null && Config.EnableModules) {
				if (ModuleLoader.LoadModules().Item1) {
					Logger.Log("Failed to load modules.", LogLevels.Warn);
				}
			}
			else {
				Logger.Log("Could not start the modules as network is unavailable or modules is not initilized.", LogLevels.Warn);
			}
		}

		public static async Task OnExit() {
			Logger.Log("Shutting down...");
			if (Update != null) {
				Update.StopUpdateTimer();
				Logger.Log("Update timer disposed!", LogLevels.Trace);
			}

			if (RefreshConsoleTitleTimer != null) {
				RefreshConsoleTitleTimer.Dispose();
				Logger.Log("Console title refresh timer disposed!", LogLevels.Trace);
			}

			if (ConfigWatcher.ConfigWatcherOnline) {
				ConfigWatcher.StopConfigWatcher();
			}

			if (ModuleWatcher != null && ModuleWatcher.ModuleWatcherOnline) {
				ModuleWatcher.StopModuleWatcher();
			}

			if (AssistantStatus != null) {
				AssistantStatus.Dispose();
			}

			if (KestrelServer.IsServerOnline) {
				await KestrelServer.Stop().ConfigureAwait(false);
			}

			if (ModuleLoader != null) {
				_ = await ModuleLoader.OnCoreShutdown().ConfigureAwait(false);
			}

			if (Controller != null) {
				await Controller.InitShutdown().ConfigureAwait(false);
			}

			if (Config != null) {
				Config.ProgramLastShutdown = DateTime.Now;
				Config.SaveConfig(Config);
			}

			Logger.Log("Finished on exit tasks...", LogLevels.Trace);
		}

		public static async Task Exit(bool quickShutdown) {
			if (quickShutdown) {
				GracefullModuleShutdown = false;
				await OnExit().ConfigureAwait(false);
				Logger.Log("Bye, have a good day sir!");
				Logging.LoggerOnShutdown();
				Environment.Exit(0);
			}
		}

		public static async Task Exit(int exitCode = 0) {
			if (exitCode != 0) {
				GracefullModuleShutdown = false;
				Logger.Log("Exiting with nonzero error code...", LogLevels.Error);
			}

			if (exitCode == 0) {
				GracefullModuleShutdown = true;
				await OnExit().ConfigureAwait(false);
			}

			Logger.Log("Bye, have a good day sir!");
			Logging.LoggerOnShutdown();
			Environment.Exit(exitCode);
		}

		public static async Task Restart(int delay = 10) {
			if (!Config.AutoRestart) {
				Logger.Log("Auto restart is turned off in config.", LogLevels.Warn);
				return;
			}

			Helpers.ScheduleTask(() => Helpers.ExecuteCommand("cd /home/pi/Desktop/HomeAssistant/Helpers/Restarter && dotnet RestartHelper.dll", false), TimeSpan.FromSeconds(delay));
			await Exit(0).ConfigureAwait(false);
		}
	}
}
