using Luna.Gpio.Controllers;
using Luna.Gpio.Drivers;
using Luna.Gpio.Exceptions;
using Luna.Gpio.PinEvents;
using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Gpio {
	internal class GpioCore : IDisposable {
		private readonly InternalLogger Logger = new InternalLogger(nameof(GpioCore));
		private readonly bool IsGracefullShutdownRequested = true;
		private readonly PinsWrapper Pins;
		private static bool IsInitSuccess;

		private readonly InternalEventGenerator EventGenerator;
		private readonly MorseRelayTranslator MorseTranslator;
		private readonly BluetoothController BluetoothController;
		private readonly SoundController SoundController;
		private readonly PinController PinController;		
		private readonly Core Core;

		private PinConfig PinConfig;
		internal static readonly bool IsAllowedToExecute;

		static GpioCore() {
			IsAllowedToExecute = RuntimeInformation.ProcessArchitecture == Architecture.Arm && RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		}

		internal GpioCore(PinsWrapper pins, Core core, bool shouldGracefullyShutdown = true) {
			Core = core ?? throw new ArgumentNullException(nameof(core));
			Pins = pins;
			IsGracefullShutdownRequested = shouldGracefullyShutdown;
			PinController = new PinController(this);
			EventGenerator = new InternalEventGenerator(Core, PinController.GetDriver());
			MorseTranslator = new MorseRelayTranslator(this);
			BluetoothController = new BluetoothController(this);
			SoundController = new SoundController(this);
			PinConfig = new PinConfig();
		}

		internal async Task InitController(GpioControllerDriver? driver, NumberingScheme numberingScheme) {
			if (IsInitSuccess || driver == null) {
				return;
			}

			if (!IsAllowedToExecute) {
				Logger.Warn("Running OS platform is unsupported.");
				return;
			}

			PinController.InitPinController(driver);
			GpioControllerDriver? gpioDriver = PinController.GetDriver();

			if (driver == null || !driver.IsDriverInitialized) {
				Logger.Warn($"{gpioDriver.DriverName} failed to initialize properly. Restart of entire application is recommended.");
				throw new DriverInitializationFailedException(gpioDriver.DriverName.ToString());
			}

			if (!await PinConfig.LoadConfiguration().ConfigureAwait(false)) {
				GeneratePinConfiguration();
				await PinConfig.SaveConfig().ConfigureAwait(false);
			}

			EventGenerator.InitEventGeneration();
			IsInitSuccess = true;
		}

		private void GeneratePinConfiguration() {
			List<Pin> pinConfigs = new List<Pin>();

			for (int i = 0; i < Constants.BcmGpioPins.Length; i++) {
				pinConfigs.Add(PinController.GetDriver().GetPinConfig(Constants.BcmGpioPins[i]));
				Logger.Trace($"Generated pin config for '{Constants.BcmGpioPins[i]}' gpio pin.");
			}

			PinConfig = new PinConfig(pinConfigs, Core.Config.GpioConfiguration.GpioSafeMode);
		}

		internal InternalEventGenerator GetEventManager() => EventGenerator;

		internal MorseRelayTranslator GetMorseTranslator() => MorseTranslator;

		internal BluetoothController GetBluetoothController() => BluetoothController;

		internal SoundController GetSoundController() => SoundController;

		internal PinController GetPinController() => PinController;

		internal PinsWrapper GetAvailablePins() => Pins;

		internal PinConfig GetPinConfig() => PinConfig;

		public void Dispose() {
			EventGenerator?.Dispose();
			SoundController?.Dispose();
			PinController.GetDriver().ShutdownDriver(IsGracefullShutdownRequested);
		}
	}
}
