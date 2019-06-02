using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules;
using HomeAssistant.Server;
using HomeAssistant.Update;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.WiringPi;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant {

	public class Program {
		private static Logger Logger;
		public static GPIOController Controller;
		public static Updater Update = new Updater();
		public static TCPServer CoreServer = new TCPServer();
		public static CoreConfig Config = new CoreConfig();
		private static GPIOConfigHandler GPIOConfigHandler = new GPIOConfigHandler();
		private static GPIOConfigRoot GPIORootObject = new GPIOConfigRoot();
		private static List<GPIOPinConfig> GPIOConfig = new List<GPIOPinConfig>();
		public static ModuleInitializer Modules;

		public static readonly string ProcessFileName = Process.GetCurrentProcess().MainModule.FileName;
		public static DateTime StartupTime;
		private static Timer RefreshConsoleTitleTimer;
		public static bool CoreInitiationCompleted = false;
		public static bool DisablePiMethods = false;

		// Handle Pre-init Tasks in here
		private static async Task Main(string[] args) {
			Helpers.CheckMultipleProcess();

			if (!Helpers.IsRaspberryEnvironment()) {
				DisablePiMethods = true;
			}

			Logger = new Logger("CORE");
			TaskScheduler.UnobservedTaskException += HandleTaskExceptions;
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledExceptions;
			AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceExceptions;
			StartupTime = DateTime.Now;
			bool Init = await InitCore(args).ConfigureAwait(false);
		}

		private static async Task<bool> InitCore(string[] args) {
			try {
				await Helpers.DisplayTessASCII().ConfigureAwait(false);
				Helpers.InBackgroundThread(SetConsoleTitle, "Console Title Updater");
				Logger.Log($"X--------  Starting TESS Assistant v{Constants.Version}  --------X", LogLevels.Ascii);
				Logger.Log("Loading core config...", LogLevels.Trace);
				try {
					Config = Config.LoadConfig();
				}
				catch (NullReferenceException) {
					Logger.Log("Fatal error has occured during loading Core Config. exiting...", LogLevels.Error);
					await Exit(1).ConfigureAwait(false);
					return false;
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

				Config.ProgramLastStartup = StartupTime;
				Logger.Log("Verifying internet connectivity...", LogLevels.Trace);

				if (Helpers.CheckForInternetConnection()) {
					Logger.Log("Internet connection verified!", LogLevels.Trace);
				}
				else {
					Logger.Log("No internet connection detected!");
					Logger.Log("To continue with initialization, press c else press q to quit.");
					ConsoleKeyInfo key = Console.ReadKey();

					switch (key.KeyChar) {
						case 'c':
							break;

						case 'q':
							await Exit(0).ConfigureAwait(false);
							break;

						default:
							Logger.Log("Unknown value entered! continuing to run the program...");
							goto case 'c';
					}
				}

				bool Token = false;

				try {
					string checkForToken = Helpers.FetchVariable(0, true, "GITHUB_TOKEN");

					if (string.IsNullOrEmpty(checkForToken) || string.IsNullOrWhiteSpace(checkForToken)) {
						Logger.Log("Github token isnt found. Updates will be disabled.", LogLevels.Warn);
						Token = false;
					}
				}
				catch (Exception) {
					Logger.Log("Github token isnt found. Updates will be disabled.", LogLevels.Warn);
					Token = false;
				}

				Token = true;

				if (Token && Config.AutoUpdates) {
					Logger.Log("Checking for any new version...", LogLevels.Trace);
					File.WriteAllText("version.txt", Constants.Version.ToString());
					await Update.CheckAndUpdate(true).ConfigureAwait(false);
				}

				if (Config.TCPServer) {
					_ = await CoreServer.StartServer().ConfigureAwait(false);
				}

				Modules = new ModuleInitializer(false);
				await Modules.StartModules().ConfigureAwait(false);
				await PostInitTasks(args).ConfigureAwait(false);
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Fatal);
				return false;
			}
			return true;
		}

		private static async Task PostInitTasks(string[] args) {
			Logger.Log("Running post-initiation tasks...", LogLevels.Trace);

			if (!DisablePiMethods) {
				Pi.Init<BootstrapWiringPi>();
				Controller = new GPIOController(args, GPIORootObject, GPIOConfig, GPIOConfigHandler);
				Controller.DisplayPiInfo();
				Logger.Log("Sucessfully Initiated Pi Configuration!");
			}
			else {
				Logger.Log("Disabled Raspberry Pi related methods and initiation tasks.");
			}

			CoreInitiationCompleted = true;

			if (Config.DisplayStartupMenu) {
				await DisplayRelayMenu().ConfigureAwait(false);
			}

			Logger.Log("Waiting for commands...");
			await KeepAlive('q', 'm', 'e', 'i', 't').ConfigureAwait(false);
		}

		private static void SetConsoleTitle() {
			Helpers.SetConsoleTitle($"{Helpers.TimeRan()} | {Helpers.GetExternalIP()}:{Config.ServerPort} | {DateTime.Now.ToLongTimeString()} | Uptime: {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 2)} minutes");

			if (RefreshConsoleTitleTimer == null) {
				RefreshConsoleTitleTimer = new Timer(e => SetConsoleTitle(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
			}
		}

		private static async Task KeepAlive(char loopBreaker = 'q', char menuKey = 'm', char quickShutDown = 'e', char imapIdleShutdown = 'i', char testKey = 't') {
			Logger.Log($"Press {loopBreaker} to quit in 5 seconds.");
			Logger.Log($"Press {quickShutDown} to exit application immediately.");
			Logger.Log($"Press {menuKey} to display GPIO menu.");
			Logger.Log($"Press {imapIdleShutdown} to stop all IMAP Idle notifications.");
			Logger.Log($"Press {testKey} to execute the TEST methods.");

			while (true) {
				char pressedKey = Console.ReadKey().KeyChar;

				if (pressedKey.Equals(loopBreaker)) {
					Logger.Log("Exiting in 5 secs as per the admin.");
					await Task.Delay(5000).ConfigureAwait(false);
					await Exit(0).ConfigureAwait(false);
				}
				else if (pressedKey.Equals(menuKey)) {
					Logger.Log("Displaying relay testing menu...");
					await DisplayRelayMenu().ConfigureAwait(false);
					continue;
				}
				else if (pressedKey.Equals(quickShutDown)) {
					Logger.Log("Exiting...");
					await Exit(0).ConfigureAwait(false);
				}
				else if (pressedKey.Equals(imapIdleShutdown)) {
					Logger.Log("Exiting IMAP Idle...");
					Modules.DisposeAllEmailClients();
				}
				else if (pressedKey.Equals(testKey)) {
					Logger.Log("Running pre-configured tests...");
				}
				else {
					continue;
				}
			}
		}

		public static async Task DisplayRelayMenu() {
			if (DisablePiMethods) {
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
					else {
						Logger.Log("Test sucessfull!");
						Logger.Log($"Press q to quit in 5 seconds.");
						Logger.Log($"Press m to exit application immediately.");
						Logger.Log($"Press e to display GPIO menu.");
						Logger.Log($"Press i to stop IMAP Idle notifications.");
					}

					break;

				case 2:
					Configured = await Controller.RelayTestService(GPIOCycles.OneMany).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					else {
						Logger.Log("Test sucessfull!");
						Logger.Log($"Press q to quit in 5 seconds.");
						Logger.Log($"Press m to exit application immediately.");
						Logger.Log($"Press e to display GPIO menu.");
						Logger.Log($"Press i to stop IMAP Idle notifications.");
					}

					break;

				case 3:
					Configured = await Controller.RelayTestService(GPIOCycles.OneOne).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					else {
						Logger.Log("Test sucessfull!");
						Logger.Log($"Press q to quit in 5 seconds.");
						Logger.Log($"Press m to exit application immediately.");
						Logger.Log($"Press e to display GPIO menu.");
						Logger.Log($"Press i to stop IMAP Idle notifications.");
					}

					break;

				case 4:
					Configured = await Controller.RelayTestService(GPIOCycles.OneTwo).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					else {
						Logger.Log("Test sucessfull!");
						Logger.Log($"Press q to quit in 5 seconds.");
						Logger.Log($"Press m to exit application immediately.");
						Logger.Log($"Press e to display GPIO menu.");
						Logger.Log($"Press i to stop IMAP Idle notifications.");
					}

					break;

				case 5:
					Logger.Log("\nPlease select the channel (2, 3, 4, 17, 27, 22, 10, 9, etc): ", LogLevels.UserInput);
					ConsoleKeyInfo singleKey = Console.ReadKey();
					int selectedsingleKey;

					if (!int.TryParse(singleKey.KeyChar.ToString(), out selectedsingleKey)) {
						Logger.Log("Could not prase the input key. please try again!", LogLevels.Error);
						goto case 5;
					}

					Configured = await Controller.RelayTestService(GPIOCycles.Single, selectedsingleKey).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					else {
						Logger.Log("Test sucessfull!");
						Logger.Log($"Press q to quit in 5 seconds.");
						Logger.Log($"Press m to exit application immediately.");
						Logger.Log($"Press e to display GPIO menu.");
						Logger.Log($"Press i to stop IMAP Idle notifications.");
					}

					break;

				case 6:
					Configured = await Controller.RelayTestService(GPIOCycles.Default).ConfigureAwait(false);

					if (!Configured) {
						Logger.Log("Could not configure the setting. please try again!", LogLevels.Warn);
					}
					else {
						Logger.Log("Test sucessfull!");
						Logger.Log($"Press q to quit in 5 seconds.");
						Logger.Log($"Press m to exit application immediately.");
						Logger.Log($"Press e to display GPIO menu.");
						Logger.Log($"Press i to stop IMAP Idle notifications.");
					}

					break;

				case 0:
					Logger.Log("Exiting from menu...", LogLevels.UserInput);
					return;

				default:
					goto case 0;
			}
		}

		public static void HandleTaskExceptions(object sender, UnobservedTaskExceptionEventArgs e) {
			Logger.Log($"{e.Exception.InnerException}/{e.Exception.Message}/{e.Exception.TargetSite}", LogLevels.Warn);
			Logger.Log($"{e.Exception.ToString()}", LogLevels.Trace);
		}

		public static void HandleFirstChanceExceptions(object sender, FirstChanceExceptionEventArgs e) {
			if (!Config.Debug) {
				return;
			}
			if (e.Exception is PlatformNotSupportedException || e.Exception is OperationCanceledException || e.Exception is SocketException) {
				Logger.Log(e.Exception.ToString(), LogLevels.Trace);
			}
			else {
				//Logger.Log($"{e.Exception.Source}/{e.Exception.Message}/{e.Exception.TargetSite}/{e.Exception.StackTrace}", LogLevels.Warn);
				Logger.Log(e.Exception, ExceptionLogLevels.Fatal);
			}
		}

		public static void HandleUnhandledExceptions(object sender, UnhandledExceptionEventArgs e) {
			Logger.Log($"{e.ExceptionObject.ToString()}", LogLevels.Error);
			Logger.Log($"{e.ToString()}", LogLevels.Trace);
		}

		public static async Task OnExit() {
			if (Modules != null) {
				await Modules.OnCoreShutdown().ConfigureAwait(false);
			}

			if (Config != null) {
				Config.ProgramLastShutdown = DateTime.Now;
				Config.SaveConfig(Config);
			}

			if (Controller != null) {
				await Controller.InitShutdown().ConfigureAwait(false);
			}

			if (CoreServer != null && CoreServer.ServerOn) {
				await CoreServer.StopServer().ConfigureAwait(false);
			}

			if (RefreshConsoleTitleTimer != null) {
				RefreshConsoleTitleTimer.Dispose();
			}

			if (Update != null) {
				Update.StopUpdateTimer();
			}
		}

		public static async Task Exit(byte exitCode = 0) {
			if (exitCode != 0) {
				Logger.Log("Exiting with nonzero error code...", LogLevels.Error);
			}

			if (exitCode == 0) {
				await OnExit().ConfigureAwait(false);
			}

			Logger.Log("Shutting down...");
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
