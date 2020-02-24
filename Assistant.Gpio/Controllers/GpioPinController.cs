using Assistant.Gpio.Config;
using Assistant.Gpio.Drivers;
using Assistant.Gpio.Events;
using Assistant.Gpio.Events.EventArgs;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Assistant.Gpio.Config.PinConfig;
using static Assistant.Gpio.Controllers.PiController;

namespace Assistant.Gpio.Controllers {
	public class GpioPinController : IGpioControllerDriver {
		public static readonly ILogger Logger = new Logger(typeof(GpioPinController).Name);
		public EGPIO_DRIVERS CurrentGpioDriver { get; private set; }
		private IGpioControllerDriver GpioControllerDriver { get; set; } = new NullDriver();
		private GpioEventManager? GpioPollingManager => Controller?.GetEventManager();
		private readonly PiController Controller;

		public GpioPinController(PiController controller) => Controller = controller;

		public GpioPinController InitGpioController(EGPIO_DRIVERS driver) {
			if (!IsAllowedToExecute) {
				return this;
			}

			CurrentGpioDriver = driver;

			switch (CurrentGpioDriver) {
				case EGPIO_DRIVERS.RaspberryIODriver:
					GpioControllerDriver = new RaspberryIODriver().InitDriver();
					Controller.SetEventManager(new GpioEventManager(Controller, this));
					break;
				case EGPIO_DRIVERS.GpioDevicesDriver:
					GpioControllerDriver = new SystemDeviceDriver().InitDriver(System.Device.Gpio.PinNumberingScheme.Logical);
					Controller.SetEventManager(new GpioEventManager(Controller, this));
					break;
				case EGPIO_DRIVERS.WiringPiDriver:
				default:
					Logger.Info("Currently, only RaspberryIO Driver is supported.");
					break;
			}

			Logger.Log($"Gpio Controller initiated with {CurrentGpioDriver.ToString()} driver.");
			return this;
		}

		public void StartInternalPinPolling(int[] outputPins) {
			if (GpioPollingManager == null) {
				Logger.Warning("Internal polling failed as polling manager is malfunctioning.");
				return;
			}

			if (outputPins.Length <= 0) {
				Logger.Warning("Pin array is empty!");
				return;
			}

			foreach (int pin in outputPins) {
				GpioPinEventConfig config = new GpioPinEventConfig(pin, GpioPinMode.Output, GpioPinEventStates.ALL);
				GpioPollingManager.RegisterGpioEvent(config);
				RegisterEventDelegate(pin, OnRelayPinValueChanged);
			}
		}

		public bool RegisterEventDelegate(int pin, GpioEventGenerator.GpioPinValueChangedEventHandler eventHandler) {
			if (GpioPollingManager == null || GpioPollingManager.GpioPinEventGenerators.Count <= 0 || eventHandler == null) {
				return false;
			}

			foreach (GpioEventGenerator i in GpioPollingManager.GpioPinEventGenerators) {
				if (i.IsEventRegistered && i.EventPinConfig.GpioPin == pin) {
					i.GpioPinValueChanged += eventHandler;
					return true;
				}
			}

			return false;
		}

		private void OnIrSensorValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			if (e == null || sender == null) {
				return;
			}

			switch (e.PinState) {
				case GpioPinState.On:
					Logger.Log($"An Object is in front of the sensor! Pin -> {e.PinNumber}");
					break;
				case GpioPinState.Off:
					Logger.Log($"No objects detected! Pin -> {e.PinNumber}");
					break;
				default:
					break;
			}
		}

		private void OnRelayPinValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			if (e == null || sender == null) {
				return;
			}

			switch (e.PinState) {
				case GpioPinState.On:
					Logger.Info($"Relay module connected to {e.PinNumber} gpio pin set to ON state. (OFF)");
					break;

				case GpioPinState.Off:
					Logger.Log($"Relay module connected to {e.PinNumber} gpio pin set to OFF state. (ON)");
					break;

				default:
					break;
			}
		}

		private void OnSoundSensorValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			if (e == null || sender == null) {
				return;
			}

			switch (e.PinState) {
				case GpioPinState.On:
					Logger.Log($"Sound detected! Pin -> {e.PinNumber}");
					break;

				case GpioPinState.Off:
					Logger.Trace($"No sound. Pin -> {e.PinNumber}");
					break;

				default:
					break;
			}
		}

		public bool IsDriverProperlyInitialized => GetDriver().IsDriverProperlyInitialized;

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		private IGpioControllerDriver GetDriver() {
			switch (CurrentGpioDriver) {
				case EGPIO_DRIVERS.RaspberryIODriver:
					if (GpioControllerDriver != null && GpioControllerDriver.IsDriverProperlyInitialized) {
						return GpioControllerDriver;
					}
					break;
				default:
					throw new InvalidOperationException("Internal error with the drivers.");
			}

			return GpioControllerDriver ?? new NullDriver();
		}

		public Pin GetPinConfig(int pinNumber) => GetDriver().GetPinConfig(pinNumber);

		public bool SetGpioValue(int pin, GpioPinMode mode) => GetDriver().SetGpioValue(pin, mode);

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) => GetDriver().SetGpioValue(pin, mode, state);

		public bool SetGpioValue(int pin, GpioPinState state) => GetDriver().SetGpioValue(pin, state);

		public bool SetGpioWithTimeout(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration) => GetDriver().SetGpioWithTimeout(pin, mode, state, duration);

		public void ShutdownDriver() => GetDriver().ShutdownDriver();

		public async Task<bool> RelayTestAsync(IEnumerable<int> relayPins, GpioCycles selectedCycle, int singleChannelValue = 0) => await GetDriver().RelayTestAsync(relayPins, selectedCycle, singleChannelValue).ConfigureAwait(false);

		public void UpdatePinConfig(Pin pin) => GetDriver().UpdatePinConfig(pin);

		public GpioPinState GpioPinStateRead(int pin) => GetDriver().GpioPinStateRead(pin);

		public bool GpioDigitalRead(int pin) => GetDriver().GpioDigitalRead(pin);

		public int GpioPhysicalPinNumber(int bcmPin) => GetDriver().GpioPhysicalPinNumber(bcmPin);

		public IGpioControllerDriver? CastDriver<T>(T driver) where T : IGpioControllerDriver {
			return driver;
		}
	}
}
