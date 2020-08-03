using Luna.ExternalExtensions;
using Luna.Gpio.Config;
using Luna.Gpio.Controllers;
using Luna.Gpio.Drivers;
using Luna.Gpio.Events;
using Luna.Gpio.Events.EventArgs;
using Luna.Gpio.Exceptions;
using Luna.Logging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Gpio {
	internal class GpioCore {
		private readonly InternalLogger Logger = new InternalLogger(nameof(GpioCore));
		private readonly bool IsGracefullShutdownRequested = true;
		private readonly PinsWrapper AvailablePins;
		private static bool IsInitSuccess;

		private readonly EventManager EventManager;
		private readonly MorseRelayTranslator MorseTranslator;
		private readonly BluetoothController BluetoothController;
		private readonly SoundController SoundController;
		private readonly PinController PinController;
		private readonly PinConfigManager ConfigManager;

		internal static readonly bool IsAllowedToExecute;

		static GpioCore() {
			IsAllowedToExecute = RuntimeInformation.ProcessArchitecture == Architecture.Arm && RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		}

		internal GpioCore(PinsWrapper pins, bool shouldGracefullyShutdown = true) {
			AvailablePins = pins;
			IsGracefullShutdownRequested = shouldGracefullyShutdown;			
			PinController = new PinController(this);
			EventManager = new EventManager(this);
			MorseTranslator = new MorseRelayTranslator(this);
			BluetoothController = new BluetoothController(this);
			SoundController = new SoundController(this);
			ConfigManager = new PinConfigManager(this);
		}

		internal async Task InitController(GpioControllerDriver? gpioDriver, NumberingScheme numberingScheme) {
			if (IsInitSuccess || gpioDriver == null) {
				return;
			}

			if (!IsAllowedToExecute) {
				Logger.Warn("Running OS platform is unsupported.");
				return;
			}

			PinController.InitPinController(gpioDriver, numberingScheme);
			GpioControllerDriver? driver = PinController.GetDriver();

			if (driver == null || !driver.IsDriverInitialized) {
				Logger.Warn($"{gpioDriver.DriverName} failed to initialize properly. Restart of entire application is recommended.");
				throw new DriverInitializationFailedException(gpioDriver.DriverName.ToString());
			}

			GeneratePinConfiguration(driver);
			await InitEvents().ConfigureAwait(false);
			IsInitSuccess = true;
		}

		private void GeneratePinConfiguration(GpioControllerDriver _driver) {
			List<Pin> pinConfigs = new List<Pin>();

			for (int i = 0; i < Constants.BcmGpioPins.Length; i++) {
				pinConfigs.Add(_driver.GetPinConfig(Constants.BcmGpioPins[i]));
				Logger.Trace($"Generated pin config for '{Constants.BcmGpioPins[i]}' gpio pin.");
			}

			ConfigManager.Init(new PinConfig(pinConfigs, false));
		}

		private async Task InitEvents() {
			for (int i = 0; i < AvailablePins.InputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.InputPins[i], GpioPinMode.Input,
					PinEventState.Both, OnPinValueChanged);
				await EventManager.RegisterEvent(config).ConfigureAwait(false);
			}

			for (int i = 0; i < AvailablePins.OutputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.OutputPins[i], GpioPinMode.Output,
					PinEventState.Both, OnPinValueChanged);
				await EventManager.RegisterEvent(config).ConfigureAwait(false);
			}
		}

		private bool OnPinValueChanged(OnValueChangedEventArgs args) {
			return true;
		}

		public async Task Shutdown() {
			if (!IsInitSuccess) {
				return;
			}

			EventManager.StopAllEventGenerators();
			PinController.GetDriver()?.ShutdownDriver(IsGracefullShutdownRequested);
			//await ConfigManager.SaveConfig().ConfigureAwait(false);
		}

		public EventManager GetEventManager() => EventManager;

		public MorseRelayTranslator GetMorseTranslator() => MorseTranslator;

		public BluetoothController GetBluetoothController() => BluetoothController;

		public SoundController GetSoundController() => SoundController;

		public PinController GetPinController() => PinController;

		public PinsWrapper GetAvailablePins() => AvailablePins;
	}
}
