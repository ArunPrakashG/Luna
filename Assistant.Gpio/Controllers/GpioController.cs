using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Drivers;
using Assistant.Gpio.Events;
using Assistant.Gpio.Events.EventArgs;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Controllers {
	public class GpioController {
		private static readonly ILogger Logger = new Logger(typeof(GpioController).Name);
		public readonly EGPIO_DRIVERS GpioDriver = EGPIO_DRIVERS.RaspberryIODriver;
		private static bool IsAlreadyInit;
		public static AvailablePins AvailablePins { get; private set; }
		internal static bool IsGracefullShutdownRequested = true;
		public static bool IsAllowedToExecute => IsPiEnvironment();
		private static EventManager? EventManager;
		private static MorseRelayTranslator? MorseTranslator;
		private static BluetoothController? BluetoothController;
		private static SoundController? SoundController;
		private static IOController? PinController;
		private static PinConfigManager? ConfigManager;
		private static readonly SensorEvents SensorEvents = new SensorEvents(Logger);

		public GpioController(EGPIO_DRIVERS driverToUse, AvailablePins pins, bool shouldShutdownGracefully) {
			GpioDriver = driverToUse;
			AvailablePins = pins;
			IsGracefullShutdownRequested = shouldShutdownGracefully;
		}

		public async Task InitController() {
			if (IsAlreadyInit) {
				return;
			}

			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				return;
			}

			PinController = new IOController();

			switch (GpioDriver) {
				case EGPIO_DRIVERS.RaspberryIODriver:
					PinController.InitIoController<RaspberryIODriver>(new RaspberryIODriver(), NumberingScheme.Logical);
					break;
				case EGPIO_DRIVERS.SystemDevicesDriver:
					PinController.InitIoController<SystemDeviceDriver>(new SystemDeviceDriver(), NumberingScheme.Logical);
					break;
				case EGPIO_DRIVERS.WiringPiDriver:
					PinController.InitIoController<WiringPiDriver>(new WiringPiDriver(), NumberingScheme.Logical);
					break;
			}

			IGpioControllerDriver? driver = IOController.GetDriver();

			if (driver == null || !driver.IsDriverProperlyInitialized) {
				Logger.Warning("Failed to initialize pin controller and its drivers.");
				return;
			}

			EventManager = new EventManager();
			MorseTranslator = new MorseRelayTranslator();
			BluetoothController = new BluetoothController();
			SoundController = new SoundController();

			SetEvents();

			if (ConfigManager != null && PinConfigManager.GetConfiguration().PinConfigs.Count <= 0) {
				await ConfigManager.LoadConfiguration().ConfigureAwait(false);
			}

			IsAlreadyInit = true;
		}

		private void SetEvents() {
			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				return;
			}

			IGpioControllerDriver? driver = IOController.GetDriver();

			if (driver == null || !driver.IsDriverProperlyInitialized) {
				Logger.Warning("Failed to set events as drivers are not loaded.");
				return;
			}

			List<Pin> pinConfigs = new List<Pin>();
			for (int i = 0; i < Constants.BcmGpioPins.Length; i++) {
				Pin? config = driver?.GetPinConfig(Constants.BcmGpioPins[i]);

				if (config == null) {
					continue;
				}

				pinConfigs.Add(config);
				Logger.Trace($"Generated pin config for {Pi.Gpio[i].PhysicalPinNumber} gpio pin.");
			}

			ConfigManager = new PinConfigManager().Init(new PinConfig(pinConfigs));

			for (int i = 0; i < AvailablePins.InputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.InputPins[i], GpioPinMode.Input,
					GpioPinEventStates.ALL, GetSensorType(AvailablePins.InputPins[i]));
				EventManager.RegisterEvent(config);
			}

			for (int i = 0; i < AvailablePins.OutputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.OutputPins[i], GpioPinMode.Output,
					GpioPinEventStates.ALL, GetSensorType(AvailablePins.OutputPins[i]));
				EventManager.RegisterEvent(config);
			}
			
			MapInternalSensors();
		}

		private void MapInternalSensors() {
			for (int i = 0; i < PinConfigManager.GetConfiguration().PinConfigs.Count; i++) {
				Pin? config = PinConfigManager.GetConfiguration().PinConfigs[i];

				if (config == null) {
					continue;
				}

				SensorType sensorType = GetSensorType(Constants.BcmGpioPins[i]);
				switch (sensorType) {
					case SensorType.Buzzer:
					case SensorType.Invalid:
						break;
					case SensorType.IRSensor:
						IOController.GetDriver()?.MapSensor<IIRSensor>(new SensorMap<IIRSensor>(config.PinNumber,
							MappingEvent.Both, sensorType, SensorEvents.IrSensorEvent));
						break;
					case SensorType.Relay:
						IOController.GetDriver()?.MapSensor<IRelaySwitch>(new SensorMap<IRelaySwitch>(config.PinNumber,
							MappingEvent.Both, sensorType, SensorEvents.RelaySwitchEvent));
						break;
					case SensorType.SoundSensor:
						IOController.GetDriver()?.MapSensor<ISoundSensor>(new SensorMap<ISoundSensor>(config.PinNumber,
							MappingEvent.Both, sensorType, SensorEvents.SoundSensorEvent));
						break;
				}

				Logger.Trace($"Sensor Map generated for '{config.PinNumber}' / '{sensorType}' gpio pin.");
			}
		}

		public static SensorType GetSensorType(int gpioPin) {
			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				return SensorType.Invalid;
			}

			if (!IOController.IsValidPin(gpioPin)) {
				Logger.Warning($"Invalid gpio pin. '{gpioPin}'");
				return SensorType.Invalid;
			}

			if (AvailablePins.IrSensorPins.Contains(gpioPin)) {
				return SensorType.IRSensor;
			}

			if (AvailablePins.RelayPins.Contains(gpioPin)) {
				return SensorType.Relay;
			}

			if (AvailablePins.SoundSensorPins.Contains(gpioPin)) {
				return SensorType.SoundSensor;
			}

			// TODO: we are purposly ignoring other sensors which can be connected.
			// NEED REWORK.
			return SensorType.Invalid;
		}

		public void Shutdown() {
			if (!IsAlreadyInit) {
				return;
			}

			ConfigManager?.SaveConfig().ConfigureAwait(false);
			EventManager?.StopAllEventGenerators();
			IOController.GetDriver()?.ShutdownDriver();
		}

		private static bool IsPiEnvironment() {
			if (Helpers.GetOsPlatform() == OSPlatform.Linux) {
				return Pi.Info.RaspberryPiVersion.ToString().Equals("Pi3ModelBEmbest", StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		public static EventManager? GetEventManager() => EventManager;
		public static MorseRelayTranslator? GetMorseTranslator() => MorseTranslator;
		public static BluetoothController? GetBluetoothController() => BluetoothController;
		public static SoundController? GetSoundController() => SoundController;
		public static IOController? GetPinController() => PinController;
	}
}
