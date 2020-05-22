using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Drivers;
using Assistant.Gpio.Events;
using Assistant.Gpio.Events.EventArgs;
using Assistant.Gpio.Exceptions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio {
	public class GpioCore {
		private readonly ILogger Logger = new Logger(typeof(GpioCore).Name);
		private readonly bool IsGracefullShutdownRequested = true;
		private readonly AvailablePins AvailablePins;
		private static bool IsInitSuccess;

		private readonly EventManager EventManager;
		private readonly MorseRelayTranslator MorseTranslator;
		private readonly BluetoothController BluetoothController;
		private readonly SoundController SoundController;
		private readonly PinController PinController;
		private readonly PinConfigManager ConfigManager;

		public static bool IsAllowedToExecute { get; private set; }

		public GpioCore(AvailablePins _availablePins, bool _gracefullShutdownRequested = true) {
			AvailablePins = _availablePins;
			IsGracefullShutdownRequested = _gracefullShutdownRequested;

			PinController = new PinController(this);
			EventManager = new EventManager(this);
			MorseTranslator = new MorseRelayTranslator(this);
			BluetoothController = new BluetoothController(this);
			SoundController = new SoundController(this);
			ConfigManager = new PinConfigManager(this);
		}

		public async Task InitController(IGpioControllerDriver? _driver, bool isUnixSys, NumberingScheme _scheme) {
			if (IsInitSuccess || _driver == null) {
				return;
			}

			IsAllowedToExecute = isUnixSys && Helpers.GetPlatform() == OSPlatform.Linux;			

			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				return;
			}

			PinController.InitPinController(_driver, _scheme);
			IGpioControllerDriver? driver = PinController.GetDriver();

			if (driver == null || !driver.IsDriverInitialized) {
				Logger.Warning($"{_driver.DriverName} failed to initialize properly. Restart of entire application is recommended.");
				throw new DriverInitializationFailedException(_driver.DriverName.ToString());
			}

			GeneratePinConfiguration(driver);
			await InitEvents().ConfigureAwait(false);
			IsInitSuccess = true;
		}

		private void GeneratePinConfiguration(IGpioControllerDriver _driver) {
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
					PinEventStates.Both, OnPinValueChanged);
				await EventManager.RegisterEvent(config).ConfigureAwait(false);
			}

			for (int i = 0; i < AvailablePins.OutputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.OutputPins[i], GpioPinMode.Output,
					PinEventStates.Both, OnPinValueChanged);
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

		public AvailablePins GetAvailablePins() => AvailablePins;
	}
}
