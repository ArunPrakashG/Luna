using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio {
	public class PiController {
		internal readonly Logger Logger = new Logger("PI-CONTROLLER");
		public GpioEventManager GpioPollingManager { get; internal set; }
		public GpioMorseTranslator MorseTranslator { get; private set; }
		public BluetoothController PiBluetooth { get; private set; }
		public InputManager InputManager { get; private set; }
		public SoundController PiSound { get; private set; }
		public GpioPinController PinController { get; private set; }

		public bool IsControllerProperlyInitialized { get; private set; } = false;
		public bool EnableExperimentalFunction { get; private set; } = false;
		public readonly EGpioDriver CurrentDriver;

		public static List<GpioPinConfig> PinConfigCollection { get; private set; } = new List<GpioPinConfig>(40);

		internal PiController(EGpioDriver driver) => CurrentDriver = driver;

		internal PiController InitController() {
			//TODO: Add support for other drivers such as System.Device.Gpio and wiring pi command line etc
			if (CurrentDriver != EGpioDriver.RaspberryIODriver) {
				throw new PlatformNotSupportedException("Only GenericDriver (RaspberryIO Driver) is supported as of now.");
			}

			if (Core.DisablePiMethods || Core.RunningPlatform != OSPlatform.Linux) {
				Logger.Log("Running platform is unsupported.", LogLevels.Warn);
				IsControllerProperlyInitialized = false;
				return this;
			}

			IsControllerProperlyInitialized = true;
			PinController = new GpioPinController(CurrentDriver).InitGpioController();
			
			if (!PinController.IsDriverProperlyInitialized) {
				Logger.Log("Failed to initialize the driver.", LogLevels.Warn);
				IsControllerProperlyInitialized = false;
				return this;
			}

			MorseTranslator = new GpioMorseTranslator().InitMorseTranslator();
			PiBluetooth = new BluetoothController().InitBluetoothController();
			PiSound = new SoundController();
			InputManager = new InputManager();
			ControllerHelpers.DisplayPiInfo();
			PinConfigCollection.Clear();

			for (int i = 0; i < Constants.BcmGpioPins.Length; i++) {
				GpioPinConfig config = PinController.GetGpioConfig(Constants.BcmGpioPins[i]);
				PinConfigCollection.Add(config);
				Logger.Log($"Generated pin config for {Constants.BcmGpioPins[i]} gpio pin.", Enums.LogLevels.Trace);
			}

			IsControllerProperlyInitialized = true;
			PinController.StartInternalPinPolling();
			Logger.Log("Initiated Gpio Controller class!", Enums.LogLevels.Trace);
			return this;
		}

		public static bool IsValidPin(int pin) {
			if (Core.DisablePiMethods || pin > 40 || pin <= 0 || !Constants.BcmGpioPins.Contains(pin)) {
				return false;
			}

			return true;
		}

		public void InitGpioShutdownTasks() {
			GpioPollingManager.ExitEventGenerator();

			if (Core.Config.CloseRelayOnShutdown) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinConfig pinStatus = PinController.GetGpioConfig(pin);
					if (pinStatus.IsPinOn) {
						PinController.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					}
				}
			}

			PinController.ShutdownDriver();
		}
	}
}
