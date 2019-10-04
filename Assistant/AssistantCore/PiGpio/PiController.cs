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
		private GpioEventManager GpioPollingManager { get; set; } = new GpioEventManager();
		private GpioMorseTranslator MorseTranslator { get; set; } = new GpioMorseTranslator();
		private BluetoothController PiBluetooth { get; set; } = new BluetoothController();
		private InputManager InputManager { get; set; } = new InputManager();
		private SoundController PiSound { get; set; } = new SoundController();
		private GpioPinController PinController { get; set; } = new GpioPinController();

		public bool IsControllerProperlyInitialized { get; private set; } = false;
		public bool EnableExperimentalFunction { get; private set; } = true;
		public static EGpioDriver CurrentDriver { get; private set; } = EGpioDriver.RaspberryIODriver;

		public static List<GpioPinConfig> PinConfigCollection { get; private set; } = new List<GpioPinConfig>(40);

		internal PiController InitController(EGpioDriver driver) {
			CurrentDriver = driver;
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
			PinController.InitGpioController(CurrentDriver);

			if (!PinController.IsDriverProperlyInitialized) {
				Logger.Log("Failed to initialize the driver.", LogLevels.Warn);
				IsControllerProperlyInitialized = false;
				return this;
			}

			MorseTranslator.InitMorseTranslator();
			PiBluetooth.InitBluetoothController();
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

		public GpioEventManager GetEventManager() => GpioPollingManager;

		internal void SetEventManager(GpioEventManager manager) {
			if(manager == null) {
				return;
			}

			GpioPollingManager = manager;
		}

		public GpioMorseTranslator GetMorseTranslator() => MorseTranslator;

		public BluetoothController GetBluetoothController() => PiBluetooth;

		public InputManager GetInputManager() => InputManager;

		public SoundController GetSoundController() => PiSound;

		public GpioPinController GetPinController() => PinController;

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
