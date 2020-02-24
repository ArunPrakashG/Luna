using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Events;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unosquare.RaspberryIO;
using static Assistant.Gpio.Config.PinConfig;

namespace Assistant.Gpio.Controllers {
	public class PiController {
		internal static readonly ILogger Logger = new Logger(typeof(PiController).Name);
		public readonly GpioCore? GpioCore;
		public bool IsControllerProperlyInitialized { get; private set; } = false;
		internal static EGPIO_DRIVERS CurrentDriver { get; private set; } = EGPIO_DRIVERS.RaspberryIODriver;
		public static bool IsAllowedToExecute => GpioHelpers.IsPiEnvironment();
		internal static bool GracefullShutdown { get; private set; } = true;

		public PiController(GpioCore gpioCore) => GpioCore = gpioCore;

		internal PiController? InitController(bool gracefulExit = true) {
			if (GpioCore == null || !IsAllowedToExecute) {
				return null;
			}

			GpioCore.PinController = new GpioPinController(this);
			GpioCore.GpioPollingManager = new GpioEventManager(this, GpioCore.PinController);
			GpioCore.MorseTranslator = new GpioMorseTranslator();
			GpioCore.PiBluetooth = new BluetoothController(GpioCore);
			GpioCore.PiSound = new PiSoundController();

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

			for (int i = 0; i < Constants.BcmGpioPins.Length; i++) {
				Pin config = GpioCore.PinController.GetPinConfig(Pi.Gpio[i].PhysicalPinNumber);
				PinConfigManager.GetConfiguration().PinConfigs[i] = config;
				Logger.Trace($"Generated pin config for {Pi.Gpio[i].PhysicalPinNumber} gpio pin.");
			}

			IsControllerProperlyInitialized = true;
			GpioCore.PinController.StartInternalPinPolling(GpioCore.GetOccupiedPins().OutputPins);
			Logger.Trace("Initiated Gpio Controller class!");
			return this;
		}

		public static bool IsValidPin(int pin) {
			if (!IsAllowedToExecute || !Constants.BcmGpioPins.Contains(pin)) {
				return false;
			}

			try {
				if (!Pi.Gpio.Contains(Pi.Gpio[pin])) {
					Logger.Warning($"pin {pin} doesn't exist or is not a valid Bcm Gpio pin.");
					return false;
				}
			}
			catch (Exception e) {
				Logger.Exception(e);
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

		public PiSoundController? GetSoundController() => GpioCore?.PiSound;

		public GpioPinController? GetPinController() => GpioCore?.PinController;

		public void InitGpioShutdownTasks() {
			if (GpioCore == null) {
				return;
			}

			GpioCore.GpioPollingManager?.ExitEventGenerator();

			var pins = GpioCore.GetOccupiedPins();

			if (pins.OutputPins.Length > 0 && GracefullShutdown) {
				foreach (int pin in pins.OutputPins) {
					Pin? pinStatus = GpioCore.PinController?.GetPinConfig(pin);
					if (pinStatus == null) {
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
