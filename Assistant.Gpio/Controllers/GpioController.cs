using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Drivers;
using Assistant.Gpio.Events;
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
		private readonly ILogger Logger = new Logger(typeof(GpioController).Name);
		private static bool IsAlreadyInit;
		private readonly bool IsGracefullShutdownRequested = true;
		private static AvailablePins AvailablePins;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		private static EventManager EventManager;
		private static MorseRelayTranslator MorseTranslator;
		private static BluetoothController BluetoothController;
		private static SoundController SoundController;
		private static PinController PinController;
		private static PinConfigManager ConfigManager;
		private static SensorEvents SensorEvents;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

		public static bool IsAllowedToExecute => IsPiEnvironment();


		public GpioController(AvailablePins pins, bool shouldShutdownGracefully = true) {
			AvailablePins = pins;
			IsGracefullShutdownRequested = shouldShutdownGracefully;
			SensorEvents = new SensorEvents(Logger);
		}

		private void PostInit() {
			PinController = new PinController(this);
			EventManager = new EventManager(this);
			MorseTranslator = new MorseRelayTranslator(this);
			BluetoothController = new BluetoothController(this);
			SoundController = new SoundController(this);
			ConfigManager = new PinConfigManager(this);
		}

		public async Task InitController<T>(T selectedDriver, NumberingScheme _scheme) where T : IGpioControllerDriver {
			if (IsAlreadyInit) {
				return;
			}

			PostInit();
			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				return;
			}

			PinController.InitPinController<T>(selectedDriver, _scheme);
			IGpioControllerDriver? driver = PinController.GetDriver();

			if (driver == null || !driver.IsDriverInitialized) {
				Logger.Warning($"{selectedDriver.DriverName} failed to initialize properly. Restart of entire application is recommended.");
				return;
			}

			await InitPinConfigs().ConfigureAwait(false);
			SetEvents();
			IsAlreadyInit = true;
		}

		private async Task InitPinConfigs() {
			bool isLoadSuccess = await ConfigManager.LoadConfiguration().ConfigureAwait(false);

			if (!isLoadSuccess || PinConfigManager.GetConfiguration() == null || PinConfigManager.GetConfiguration().PinConfigs.Count <= 0 ||
				(isLoadSuccess && (PinConfigManager.GetConfiguration() == null || PinConfigManager.GetConfiguration().PinConfigs.Count <= 0))) {
				generateConfigs();
				await ConfigManager.SaveConfig().ConfigureAwait(false);
			}

			void generateConfigs() {
				List<Pin> pinConfigs = new List<Pin>();
				for (int i = 0; i < Constants.BcmGpioPins.Length; i++) {
					Pin? config = PinController.GetDriver()?.GetPinConfig(Constants.BcmGpioPins[i]);

					if (config == null) {
						continue;
					}

					pinConfigs.Add(config);
					Logger.Trace($"Generated pin config for {Pi.Gpio[i].PhysicalPinNumber} gpio pin.");
				}

				ConfigManager.Init(new PinConfig(pinConfigs));
			}
		}

		private async Task SetEvents() {
			for (int i = 0; i < AvailablePins.InputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.InputPins[i], GpioPinMode.Input,
					GpioPinEventStates.ALL, GetSensorType(AvailablePins.InputPins[i]));
				await EventManager.RegisterEvent(config).ConfigureAwait(false);
			}

			for (int i = 0; i < AvailablePins.OutputPins.Length; i++) {
				EventConfig config = new EventConfig(AvailablePins.OutputPins[i], GpioPinMode.Output,
					GpioPinEventStates.ALL, GetSensorType(AvailablePins.OutputPins[i]));
				await EventManager.RegisterEvent(config).ConfigureAwait(false);
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
						PinController.GetDriver()?.MapSensor<IIRSensor>(new SensorMap<IIRSensor>(config.PinNumber,
							MappingEvent.Both, sensorType, SensorEvents.IrSensorEvent));
						break;
					case SensorType.Relay:
						PinController.GetDriver()?.MapSensor<IRelaySwitch>(new SensorMap<IRelaySwitch>(config.PinNumber,
							MappingEvent.Both, sensorType, SensorEvents.RelaySwitchEvent));
						break;
					case SensorType.SoundSensor:
						PinController.GetDriver()?.MapSensor<ISoundSensor>(new SensorMap<ISoundSensor>(config.PinNumber,
							MappingEvent.Both, sensorType, SensorEvents.SoundSensorEvent));
						break;
				}

				Logger.Trace($"Sensor Map generated for '{config.PinNumber}' / '{sensorType}' gpio pin.");
			}
		}

		private SensorType GetSensorType(int gpioPin) {
			if (!IsAllowedToExecute) {
				Logger.Warning("Running OS platform is unsupported.");
				return SensorType.Invalid;
			}

			if (!PinController.IsValidPin(gpioPin)) {
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

		public async Task Shutdown() {
			if (!IsAlreadyInit) {
				return;
			}

			EventManager.StopAllEventGenerators();					
			PinController.GetDriver()?.ShutdownDriver(IsGracefullShutdownRequested);
			await ConfigManager.SaveConfig().ConfigureAwait(false);
		}

		private static bool IsPiEnvironment() => Helpers.GetOsPlatform() == OSPlatform.Linux;

		public static EventManager GetEventManager() => EventManager;
		public static MorseRelayTranslator GetMorseTranslator() => MorseTranslator;
		public static BluetoothController GetBluetoothController() => BluetoothController;
		public static SoundController GetSoundController() => SoundController;
		public static PinController GetPinController() => PinController;
		public static AvailablePins GetAvailablePins() => AvailablePins;
	}
}
