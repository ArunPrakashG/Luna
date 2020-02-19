using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unosquare.RaspberryIO;

namespace Assistant.Gpio {
	public class PiController {
		internal static readonly ILogger Logger = new Logger(typeof(PiController).Name);
		public readonly GpioCore? GpioCore;
		public bool IsControllerProperlyInitialized { get; private set; } = false;
		internal static EGPIO_DRIVERS CurrentDriver { get; private set; } = EGPIO_DRIVERS.RaspberryIODriver;
		public static bool IsAllowedToExecute => GpioHelpers.IsPiEnvironment();
		internal static bool GracefullShutdown { get; private set; } = true;

		[JsonProperty]
		public static List<GpioPinConfig> PinConfigCollection { get; private set; } = new List<GpioPinConfig>(40);

		public PiController(GpioCore gpioCore) => GpioCore = gpioCore;

		internal PiController? InitController(bool gracefulExit = true) {
			if(GpioCore == null || !IsAllowedToExecute) {
				return null;
			}

			GpioCore.MorseTranslator = new GpioMorseTranslator();
			GpioCore.PiBluetooth = new BluetoothController(GpioCore);
			GpioCore.PiSound = new SoundController();
			GpioCore.PinController = new GpioPinController(this);
			GpioCore.GpioPollingManager = new GpioEventManager(this, GpioCore.PinController);

			CurrentDriver = GpioCore.GpioDriver;
			GracefullShutdown = gracefulExit;

			//TODO: Add support for other drivers such as System.Device.Gpio and wiring pi command line etc
			if (CurrentDriver != EGPIO_DRIVERS.RaspberryIODriver) {
				throw new PlatformNotSupportedException("Only GenericDriver (RaspberryIO Driver) is supported as of now.");
			}

			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				IsControllerProperlyInitialized = false;
				return null;
			}

			IsControllerProperlyInitialized = true;
			GpioCore.PinController.InitGpioController(CurrentDriver);

			if (!GpioCore.PinController.IsDriverProperlyInitialized) {
				Logger.Warning("Failed to initialize the driver.");
				IsControllerProperlyInitialized = false;
				return this;
			}

			GpioCore.MorseTranslator.InitMorseTranslator(GpioCore);
			GpioCore.PiBluetooth.InitBluetoothController();
			PinConfigCollection.Clear();

			for (int i = 0; i < Pi.Gpio.Count; i++) {
				GpioPinConfig config = GpioCore.PinController.GetGpioConfig(Pi.Gpio[i].PhysicalPinNumber);
				PinConfigCollection.Add(config);
				Logger.Trace($"Generated pin config for {Pi.Gpio[i].PhysicalPinNumber} gpio pin.");
			}

			IsControllerProperlyInitialized = true;
			GpioCore.PinController.StartInternalPinPolling(GpioCore.GetOccupiedPins().output);
			Logger.Trace("Initiated Gpio Controller class!");
			return this;
		}

		public static bool IsValidPin(int pin) {
			if (!IsAllowedToExecute || pin > 41 || pin <= 0) {
				return false;
			}

			if (!Pi.Gpio.Contains(Pi.Gpio[pin])) {
				Logger.Warning($"pin {pin} doesn't exist or is not a valid Bcm Gpio pin.");
				return false;
			}

			return true;
		}

		public GpioEventManager? GetEventManager() => GpioCore?.GpioPollingManager;

		internal void SetEventManager(GpioEventManager manager) {
			if (manager == null || GpioCore == null) {
				return;
			}

			GpioCore.GpioPollingManager = manager;
		}

		public GpioMorseTranslator? GetMorseTranslator() => GpioCore?.MorseTranslator;

		public BluetoothController? GetBluetoothController() => GpioCore?.PiBluetooth;

		public SoundController? GetSoundController() => GpioCore?.PiSound;

		public GpioPinController? GetPinController() => GpioCore?.PinController;

		public void InitGpioShutdownTasks() {
			if(GpioCore == null) {
				return;
			}

			GpioCore.GpioPollingManager?.ExitEventGenerator();

			(int[] output, int[] input) = GpioCore.GetOccupiedPins();

			if(output.Length > 0 && GracefullShutdown) {
				foreach (int pin in output) {
					GpioPinConfig? pinStatus = GpioCore.PinController?.GetGpioConfig(pin);
					if(pinStatus == null) {
						continue;
					}

					if (pinStatus.IsPinOn) {
						GpioCore.PinController?.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					}
				}
			}

			GpioCore.PinController?.ShutdownDriver();
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
