using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.Gpio {
	public class PiController {
		internal static readonly ILogger Logger = new Logger("PI-CONTROLLER");
		private static GpioEventManager GpioPollingManager = new GpioEventManager();
		private static readonly GpioMorseTranslator MorseTranslator = new GpioMorseTranslator();
		private static readonly BluetoothController PiBluetooth = new BluetoothController();
		private static readonly InputManager InputManager  = new InputManager();
		private static readonly SoundController PiSound = new SoundController();
		private static readonly GpioPinController PinController = new GpioPinController();

		public bool IsControllerProperlyInitialized { get; private set; } = false;
		public bool EnableExperimentalFunction { get; private set; } = true;
		public static EGPIO_DRIVERS CurrentDriver { get; private set; } = EGPIO_DRIVERS.RaspberryIODriver;
		public static bool IsAllowedToExecute => Helpers.IsPiEnvironment();
		internal static bool GracefullShutdown { get; private set; } = true;
		private static int[] OutputPins = new int[41];
		private static int[] InputPins = new int[41];

		[JsonProperty]
		public static List<GpioPinConfig> PinConfigCollection { get; private set; } = new List<GpioPinConfig>(40);

		internal static (int[] output, int[] input) GetPins() => (OutputPins, InputPins);

		internal PiController InitController(EGPIO_DRIVERS driver, int[] outputPins, int[] inputPins, bool gracefulExit = true) {
			CurrentDriver = driver;
			GracefullShutdown = gracefulExit;
			OutputPins = outputPins;
			InputPins = inputPins;

			//TODO: Add support for other drivers such as System.Device.Gpio and wiring pi command line etc
			if (CurrentDriver != EGPIO_DRIVERS.RaspberryIODriver) {
				throw new PlatformNotSupportedException("Only GenericDriver (RaspberryIO Driver) is supported as of now.");
			}

			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				IsControllerProperlyInitialized = false;
				return this;
			}

			IsControllerProperlyInitialized = true;
			PinController.InitGpioController(CurrentDriver);

			if (!PinController.IsDriverProperlyInitialized) {
				Logger.Warning("Failed to initialize the driver.");
				IsControllerProperlyInitialized = false;
				return this;
			}

			MorseTranslator.InitMorseTranslator();
			PiBluetooth.InitBluetoothController();
			PinConfigCollection.Clear();

			for (int i = 0; i < Pi.Gpio.Count; i++) {
				GpioPinConfig config = PinController.GetGpioConfig(Pi.Gpio[i].PhysicalPinNumber);
				PinConfigCollection.Add(config);
				Logger.Trace($"Generated pin config for {Pi.Gpio[i].PhysicalPinNumber} gpio pin.");
			}

			IsControllerProperlyInitialized = true;
			PinController.StartInternalPinPolling(OutputPins);
			Logger.Trace("Initiated Gpio Controller class!");
			return this;
		}

		public static bool IsValidPin(int pin) {
			if (!IsAllowedToExecute || pin > 41 || pin <= 0) {
				return false;
			}

			if (!Pi.Gpio.Contains(Pi.Gpio[pin])) {
				Logger.Warning($"pin {pin} doesn't exist or is not a valid Bcm Gpio pin.");
			}

			return true;
		}

		public static GpioEventManager GetEventManager() => GpioPollingManager;

		internal static void SetEventManager(GpioEventManager manager) {
			if (manager == null) {
				return;
			}

			GpioPollingManager = manager;
		}

		public static GpioMorseTranslator GetMorseTranslator() => MorseTranslator;

		public static BluetoothController GetBluetoothController() => PiBluetooth;

		public static InputManager GetInputManager() => InputManager;

		public static SoundController GetSoundController() => PiSound;

		public static GpioPinController GetPinController() => PinController;

		public void InitGpioShutdownTasks() {
			GpioPollingManager.ExitEventGenerator();

			if (GracefullShutdown && OutputPins.Length > 0) {
				foreach (int pin in OutputPins) {
					GpioPinConfig pinStatus = PinController.GetGpioConfig(pin);
					if (pinStatus.IsPinOn) {
						PinController.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					}
				}
			}

			PinController.ShutdownDriver();
		}

		public enum EGPIO_DRIVERS {
			RaspberryIODriver,
			GpioDevicesDriver,
			WiringPiDriver
		}

		public enum PiAudioState {
			Mute,
			Unmute
		}

		public enum GpioPinMode {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}

		public enum GpioCycles : byte {
			Cycle,
			Single,
			Base,
			OneMany,
			OneTwo,
			OneOne,
			Default
		}

		public enum GpioPinState {
			On = 0,
			Off = 1
		}

		public enum GpioPinEventStates : byte {
			ON,
			OFF,
			ALL,
			NONE
		}
	}
}
