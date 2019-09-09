
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

using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Geolocation;
using Assistant.Log;
using Assistant.Modules;
using Assistant.MorseCode;
using Assistant.PushBullet;
using Assistant.PushBullet.ApiResponse;
using Assistant.Server;
using Assistant.Update;
using Assistant.Weather;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using Logging = Assistant.Log.Logging;

namespace Assistant.AssistantCore {

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

	public class Core {
		private static Logger Logger { get; set; }
		public static GPIOController Controller { get; private set; }
		public static Updater Update { get; private set; } = new Updater();
		public static ProcessStatus AssistantStatus { get; private set; }
		public static CoreConfig Config { get; set; } = new CoreConfig();
		public static ConfigWatcher ConfigWatcher { get; private set; } = new ConfigWatcher();
		private static ModuleWatcher ModuleWatcher { get; set; } = new ModuleWatcher();
		public static GpioConfigHandler GPIOConfigHandler { get; private set; } = new GpioConfigHandler();
		private static GpioConfigRoot GPIORootObject { get; set; } = new GpioConfigRoot();
		private static List<GpioPinConfig> GPIOConfig { get; set; } = new List<GpioPinConfig>();
		public static BluetoothController PiBluetooth { get; private set; }
		public static SoundController PiSound { get; private set; }
		public static TaskScheduler TaskManager { get; private set; } = new TaskScheduler();
		public static ModuleInitializer ModuleLoader { get; private set; }
		public static DateTime StartupTime { get; private set; }
		private static Timer RefreshConsoleTitleTimer { get; set; }
		public static DynamicWatcher DynamicWatcher { get; set; } = new DynamicWatcher();
		public static WeatherApi WeatherApi { get; private set; } = new WeatherApi();
		public static ZipCodeLocater ZipCodeLocater { get; private set; } = new ZipCodeLocater();
		public static PushBulletService PushBulletService { get; private set; }
		public static MorseCore MorseCode { get; private set; } = new MorseCore();

		public static bool CoreInitiationCompleted { get; private set; }
		public static bool DisablePiMethods { get; private set; }
		public static bool IsUnknownOs { get; set; }
		public static bool IsNetworkAvailable { get; set; }
		public static bool DisableFirstChanceLogWithDebug { get; set; }
		public static bool GracefullModuleShutdown { get; set; } = false;
		public static string AssistantName { get; set; } = "TESS Assistant";

		public static async Task<bool> InitCore(string[] args) {
			if (File.Exists(Constants.TraceLogPath)) {
				File.Delete(Constants.TraceLogPath);
			}

			Logger = new Logger("ASSISTANT");
			Config = Config.LoadConfig();
			AssistantName = Config.AssistantDisplayName;
			Logger.LogIdentifier = AssistantName;
			Helpers.CheckMultipleProcess();
			StartupTime = DateTime.Now;
			Helpers.SetFileSeperator();

			if (Helpers.CheckForInternetConnection()) {				
				IsNetworkAvailable = true;
			}
			else {
				Logger.Log("No internet connection.", Enums.LogLevels.Warn);
				Logger.Log($"Starting {AssistantName} in offline mode...");
				IsNetworkAvailable = false;
			}

			Helpers.GenerateAsciiFromText(Config.AssistantDisplayName);
			Constants.ExternelIP = Helpers.GetExternalIp();
			Constants.LocalIP = Helpers.GetLocalIpAddress();
			File.WriteAllText("version.txt", Constants.Version.ToString());

			if (Helpers.IsNullOrEmpty(Constants.ExternelIP)) {
				Constants.ExternelIP = "Failed.";
			}

			Helpers.InBackgroundThread(SetConsoleTitle, "Console Title Updater", true);
			Logger.Log($"X---------------- Starting {AssistantName} v{Constants.Version} ----------------X", Enums.LogLevels.Ascii);

			ConfigWatcher.InitConfigWatcher();
			ParseStartupArguments(args);

			if (!Helpers.IsNullOrEmpty(Config.PushBulletApiKey)) {
				PushBulletService = new PushBulletService(Config.PushBulletApiKey);
				(bool status, UserDeviceListResponse currentPushDevices) = PushBulletService.InitPushService();

				if (status) {
					Logger.Log("Push bullet service started.", Enums.LogLevels.Trace);
				}
			}

			if (!Helpers.IsRaspberryEnvironment()) {
				DisablePiMethods = true;
				IsUnknownOs = true;
			}

			GPIORootObject = GPIOConfigHandler.LoadConfig();

			if (GPIORootObject != null) {
				GPIOConfig = GPIORootObject.GPIOData;
			}

			if (Helpers.GetOsPlatform().Equals(OSPlatform.Windows)) {
				AssistantStatus = new ProcessStatus();
			}
			else {
				Logger.Log("Could not start performence counters as it is not supported on this platform.", Enums.LogLevels.Trace);
			}

			Config.ProgramLastStartup = StartupTime;

			await Update.CheckAndUpdateAsync(true).ConfigureAwait(false);

			if(Config.KestrelServer && !Helpers.IsNullOrEmpty(Config.KestrelServerUrl)) {
				await KestrelServer.Start().ConfigureAwait(false);
			}

			ModuleLoader = new ModuleInitializer();

			if (IsNetworkAvailable && Config.EnableModules) {
				(bool, LoadedModules) loadStatus = ModuleLoader.LoadModules();
				if (!loadStatus.Item1) {
					Logger.Log("Failed to load modules.", Enums.LogLevels.Warn);
				}
			}

			await PostInitTasks().ConfigureAwait(false);
			return true;
		}

		private static async Task PostInitTasks() {
			Logger.Log("Running post-initiation tasks...", Enums.LogLevels.Trace);
			ModuleWatcher.InitConfigWatcher();

			if (!DisablePiMethods && Config.EnableGpioControl) {
				Pi.Init<BootstrapWiringPi>();
				Controller = new GPIOController(GPIORootObject, GPIOConfig, GPIOConfigHandler);
				PiBluetooth = new BluetoothController();
				PiSound = new SoundController();
				ControllerHelpers.DisplayPiInfo();
				Logger.Log("Successfully Initiated Pi Configuration!");
			}
			else {
				Logger.Log("Disabled Raspberry Pi related methods and initiation tasks.");
			}

			CoreInitiationCompleted = true;
			await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.AssistantStartup).ConfigureAwait(false);

			if (Config.DisplayStartupMenu && !DisablePiMethods) {
				await DisplayRelayCycleMenu().ConfigureAwait(false);
			}

			TTSService.SpeakText($"{AssistantName} has been sucessfully started!", Enums.SpeechContext.AssistantStartup, true);
			await KeepAlive().ConfigureAwait(false);
		}

		private static void SetConsoleTitle() {
			Helpers.SetConsoleTitle($"{Helpers.TimeRan()} | {Config.KestrelServerUrl} | {DateTime.Now.ToLongTimeString()} | Uptime: {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 3)} minutes");

			if (RefreshConsoleTitleTimer == null) {
				RefreshConsoleTitleTimer = new Timer(e => SetConsoleTitle(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
			}
		}

		private static async Task DisplayConsoleCommandMenu() {
			Logger.Log("Displaying console command window", Enums.LogLevels.Trace);
			Logger.Log($"------------------------- COMMAND WINDOW -------------------------", Enums.LogLevels.UserInput);
			Logger.Log($"{Constants.ConsoleQuickShutdownKey} - Quick shutdown the assistant.", Enums.LogLevels.UserInput);
			Logger.Log($"{Constants.ConsoleDelayedShutdownKey} - Shutdown assistant in 5 seconds.", Enums.LogLevels.UserInput);

			if (!DisablePiMethods) {
				Logger.Log($"{Constants.ConsoleRelayCommandMenuKey} - Display relay pin control menu.", Enums.LogLevels.UserInput);
				Logger.Log($"{Constants.ConsoleRelayCycleMenuKey} - Display relay cycle control menu.", Enums.LogLevels.UserInput);
			}

			Logger.Log($"{Constants.ConsoleTestMethodExecutionKey} - Run pre-configured test methods or tasks.", Enums.LogLevels.UserInput);

			if (Config.EnableModules) {
				Logger.Log($"{Constants.ConsoleModuleShutdownKey} - Invoke shutdown method on all currently running modules.", Enums.LogLevels.UserInput);
			}

			if (MorseCode != null) {
				Logger.Log($"{Constants.ConsoleMorseCodeKey} - Morse code generator", Enums.LogLevels.UserInput);
			}

			if (WeatherApi != null) {
				Logger.Log($"{Constants.ConsoleWheatherInfoKey} - Get weather info", Enums.LogLevels.UserInput);
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
					case Constants.ConsoleQuickShutdownKey: {
							Logger.Log("Force quitting assistant...", Enums.LogLevels.Warn);
							await Exit(true).ConfigureAwait(false);
						}
						return;

					case Constants.ConsoleDelayedShutdownKey: {
							Logger.Log("Gracefully shutting down assistant...", Enums.LogLevels.Warn);
							GracefullModuleShutdown = true;
							await Task.Delay(5000).ConfigureAwait(false);
							await Exit(0).ConfigureAwait(false);
						}
						return;

					case Constants.ConsoleRelayCommandMenuKey when !DisablePiMethods: {
							Logger.Log("Displaying relay command menu...", Enums.LogLevels.Warn);
							DisplayRelayCommandMenu();
						}
						return;

					case Constants.ConsoleRelayCycleMenuKey when !DisablePiMethods: {
							Logger.Log("Displaying relay cycle menu...", Enums.LogLevels.Warn);
							await DisplayRelayCycleMenu().ConfigureAwait(false);
						}
						return;

					case Constants.ConsoleRelayCommandMenuKey when DisablePiMethods: {
							Logger.Log("Assistant is running in an Operating system/Device which doesnt support GPIO pin controlling functionality.", Enums.LogLevels.Warn);
						}
						return;

					case Constants.ConsoleRelayCycleMenuKey when DisablePiMethods: {
							Logger.Log("Assistant is running in an Operating system/Device which doesnt support GPIO pin controlling functionality.", Enums.LogLevels.Warn);
						}
						return;

					case Constants.ConsoleMorseCodeKey: {
							Logger.Log("Enter text to convert to morse: ");
							string morseCycle = Console.ReadLine();
							if (Controller.MorseTranslator.IsTranslatorOnline) {
								await Controller.MorseTranslator.RelayMorseCycle(morseCycle, Config.RelayPins[0], 200).ConfigureAwait(false);
							}
							else {
								Logger.Log("Could not convert due to an unknown error.", Enums.LogLevels.Warn);
							}
						}
						return;

					case Constants.ConsoleWheatherInfoKey: {
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

							if (WeatherApi != null) {
								(bool status, WeatherData response) = WeatherApi.GetWeatherInfo(Config.OpenWeatherApiKey, pinCode, "in");
								if (status) {
									Logger.Log($"------------ Weather information for {pinCode}/{response.LocationName} ------------", Enums.LogLevels.Sucess);
									Logger.Log($"Temperature: {response.Temperature}", Enums.LogLevels.Sucess);
									Logger.Log($"Humidity: {response.Temperature}", Enums.LogLevels.Sucess);
									Logger.Log($"Latitude: {response.Latitude}", Enums.LogLevels.Sucess);
									Logger.Log($"Longitude: {response.Logitude}", Enums.LogLevels.Sucess);
									Logger.Log($"Location name: {response.LocationName}", Enums.LogLevels.Sucess);
									Logger.Log($"Preasure: {response.Pressure}", Enums.LogLevels.Sucess);
									Logger.Log($"Wind speed: {response.WindDegree}", Enums.LogLevels.Sucess);

									TTSService.SpeakText($"Weather information for {response.LocationName} is", Enums.SpeechContext.Custom);
									await Task.Delay(500).ConfigureAwait(false);
									TTSService.SpeakText($"Temperature is {response.Temperature}", Enums.SpeechContext.Custom);
									await Task.Delay(500).ConfigureAwait(false);
									TTSService.SpeakText($"Humidity is {response.Humidity}", Enums.SpeechContext.Custom);
									await Task.Delay(500).ConfigureAwait(false);
									TTSService.SpeakText($"Location name is {response.LocationName}", Enums.SpeechContext.Custom);
									await Task.Delay(500).ConfigureAwait(false);
									TTSService.SpeakText($"Wind speed is {response.WindDegree}", Enums.SpeechContext.Custom);
								}
								else {
									Logger.Log("Failed to fetch wheather information, try again later!");
								}
							}
						}
						return;

					case Constants.ConsoleTestMethodExecutionKey: {
							Logger.Log("Executing test methods/tasks", Enums.LogLevels.Warn);
							Logger.Log("Test method execution finished successfully!", Enums.LogLevels.Sucess);
						}
						return;

					case Constants.ConsoleModuleShutdownKey when !ModuleLoader.LoadedModules.IsModulesEmpty && Config.EnableModules: {
							Logger.Log("Shutting down all modules...", Enums.LogLevels.Warn);
							ModuleLoader.OnCoreShutdown();
						}
						return;

					case Constants.ConsoleModuleShutdownKey when ModuleLoader.LoadedModules.IsModulesEmpty: {
							Logger.Log("There are no modules to shutdown...");
						}
						return;

					default: {
							if (failedTriesCount > maxTries) {
								Logger.Log($"Unknown key was pressed. ({maxTries - failedTriesCount} tries left)", Enums.LogLevels.Warn);
							}

							failedTriesCount++;
							continue;
						}
				}
			}
		}

		private static async Task KeepAlive() {
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", Enums.LogLevels.Sucess);

			while (true) {
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
					Logger.Log("Safe mode enabled. Only pre-configured gpio pins are allowed to be modified.", Enums.LogLevels.Warn);
					Config.GPIOSafeMode = true;
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
				Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", Enums.LogLevels.Sucess);
				return;
			}

			GpioPinConfig PinStatus;
			switch (SelectedValue) {
				case 1:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[0]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[0]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[0]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 2:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[1]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[1]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[1]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 3:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[2]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[2]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[2]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 4:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[3]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[3]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[3]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 5:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[4]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[4]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[4]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 6:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[5]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[5]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[5]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 7:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[6]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[6]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[6]} pin to ON.", Enums.LogLevels.Sucess);
					}
					break;

				case 8:
					PinStatus = Controller.FetchPinStatus(Config.RelayPins[7]);

					if (PinStatus.IsOn) {
						Controller.SetGPIO(Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.High);
						Logger.Log($"Sucessfully set {Config.RelayPins[7]} pin to OFF.", Enums.LogLevels.Sucess);
					}
					else {
						Controller.SetGPIO(Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.Low);
						Logger.Log($"Sucessfully set {Config.RelayPins[7]} pin to ON.", Enums.LogLevels.Sucess);
					}
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

					GpioPinConfig Status = Controller.FetchPinStatus(pinNumber);

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
							Logger.Log($"Sucessfully finished execution of the task: {pinNumber} pin set to OFF.", Enums.LogLevels.Sucess);
						}

						if (!Status.IsOn && pinStatus.Equals(1)) {
							Controller.SetGPIO(pinNumber, GpioPinDriveMode.Output, GpioPinValue.Low);
							Logger.Log($"Sucessfully finished execution of the task: {pinNumber} pin set to ON.", Enums.LogLevels.Sucess);
						}
					}, TimeSpan.FromMinutes(delay));

					Logger.Log(
						pinStatus.Equals(0)
							? $"Successfully scheduled a task: set {pinNumber} pin to OFF"
							: $"Successfully scheduled a task: set {pinNumber} pin to ON", Enums.LogLevels.Sucess);
					break;
			}

			Logger.Log("Command menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} for the console command menu.", Enums.LogLevels.Sucess);
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

			bool Configured;
			switch (SelectedValue) {
				case 1:
					Configured = await Controller.RelayTestService(Enums.GPIOCycles.Cycle).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}

					break;

				case 2:
					Configured = await Controller.RelayTestService(Enums.GPIOCycles.OneMany).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}

					break;

				case 3:
					Configured = await Controller.RelayTestService(Enums.GPIOCycles.OneOne).ConfigureAwait(false);
					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 4:
					Configured = await Controller.RelayTestService(Enums.GPIOCycles.OneTwo).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 5:
					Logger.Log("\nPlease select the channel (2, 3, 4, 17, 27, 22, 10, 9, etc): ", Enums.LogLevels.UserInput);
					ConsoleKeyInfo singleKey = Console.ReadKey();

					if (!int.TryParse(singleKey.KeyChar.ToString(), out int selectedsingleKey)) {
						Logger.Log("Could not prase the input key. please try again!", Enums.LogLevels.Error);
						goto case 5;
					}

					Configured = await Controller.RelayTestService(Enums.GPIOCycles.Single, selectedsingleKey).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", Enums.LogLevels.Warn);
					}
					break;

				case 6:
					Configured = await Controller.RelayTestService(Enums.GPIOCycles.Default).ConfigureAwait(false);

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

			Logger.Log(Configured ? "Test sucessfull!" : "Test Failed!");

			Logger.Log("Relay menu closed.");
			Logger.Log($"Press {Constants.ConsoleCommandMenuKey} to display command menu.");
		}

		public static async Task OnNetworkDisconnected() {
			IsNetworkAvailable = false;
			await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.NetworkDisconnected).ConfigureAwait(false);
			Constants.ExternelIP = "Internet connection lost.";

			if (Update != null) {
				Update.StopUpdateTimer();
				Logger.Log("Stopped update timer.", Enums.LogLevels.Warn);
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
			await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.NetworkReconnected).ConfigureAwait(false);
			Constants.ExternelIP = Task.Run(Helpers.GetExternalIp).Result;

			if (Config.AutoUpdates && IsNetworkAvailable) {
				Logger.Log("Checking for any new version...", Enums.LogLevels.Trace);
				File.WriteAllText("version.txt", Constants.Version.ToString());
				Update.CheckAndUpdateAsync(true);
			}

			if (!KestrelServer.IsServerOnline) {
				await KestrelServer.Start().ConfigureAwait(false);
			}

			if (IsNetworkAvailable && ModuleLoader != null && Config.EnableModules) {
				if (ModuleLoader.LoadModules().Item1) {
					Logger.Log("Failed to load modules.", Enums.LogLevels.Warn);
				}
			}
			else {
				Logger.Log("Could not start the modules as network is unavailable or modules is not initilized.", Enums.LogLevels.Warn);
			}
		}

		public static async Task OnExit() {
			Logger.Log("Shutting down...");
			if(ModuleLoader != null) {
				await ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.AssistantShutdown).ConfigureAwait(false);
			}

			if(TaskManager != null) {
				TaskManager.OnCoreShutdownRequested();
			}

			if (Update != null) {
				Update.StopUpdateTimer();
				Logger.Log("Update timer disposed!", Enums.LogLevels.Trace);
			}

			if (RefreshConsoleTitleTimer != null) {
				RefreshConsoleTitleTimer.Dispose();
				Logger.Log("Console title refresh timer disposed!", Enums.LogLevels.Trace);
			}

			if (ConfigWatcher != null && ConfigWatcher.ConfigWatcherOnline) {
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
				ModuleLoader.OnCoreShutdown();
			}

			if (Controller != null) {
				await Controller.InitShutdown().ConfigureAwait(false);
			}

			if (Config != null) {
				Config.ProgramLastShutdown = DateTime.Now;
				Config.SaveConfig(Config);
			}

			Logger.Log("Finished on exit tasks.", Enums.LogLevels.Trace);
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
				Logger.Log("Exiting with nonzero error code...", Enums.LogLevels.Error);
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
				Logger.Log("Auto restart is turned off in config.", Enums.LogLevels.Warn);
				return;
			}
			
			Helpers.ScheduleTask(() => Helpers.ExecuteCommand("cd /home/pi/Desktop/HomeAssistant/Helpers/Restarter && dotnet RestartHelper.dll", false), TimeSpan.FromSeconds(delay));
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
	}
}
