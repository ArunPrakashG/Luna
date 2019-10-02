using Assistant.AssistantCore.PiGpio.GpioControllers;
using Assistant.Log;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio {
	public class GpioPinController : IGpioControllerDriver {
		internal readonly Logger Logger = new Logger("GPIO-CONTROLLER");
		public EGpioDriver CurrentGpioDriver { get; private set; }
		private IGpioControllerDriver GpioControllerDriver { get; set; }
		private GpioEventManager GpioPollingManager => Core.PiController.GpioPollingManager;

		public GpioPinController(EGpioDriver driver) => CurrentGpioDriver = driver;

		public GpioPinController InitGpioController() {
			GpioControllerDriver = new RaspberryIOController(this).InitDriver();
			Core.PiController.GpioPollingManager = new GpioEventManager();
			Logger.Log($"Gpio Controller initiated with {CurrentGpioDriver.ToString()} driver.");
			return this;
		}

		public void StartInternalPinPolling() {
			if (Core.Config.RelayPins.Count() > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinEventConfig config = new GpioPinEventConfig(pin, GpioPinMode.Output, GpioPinEventStates.ALL);
					GpioPollingManager.RegisterGpioEvent(config);
					RegisterEventDelegate(pin, OnRelayPinValueChanged);
				}
			}

			if (Core.Config.IRSensorPins.Count() > 0) {
				foreach (int pin in Core.Config.IRSensorPins) {
					GpioPinEventConfig config = new GpioPinEventConfig(pin, GpioPinMode.Input, GpioPinEventStates.ALL);
					GpioPollingManager.RegisterGpioEvent(config);
					RegisterEventDelegate(pin, OnIrSensorValueChanged);
				}
			}

			if (Core.Config.SoundSensorPins.Count() > 0) {
				foreach (int pin in Core.Config.SoundSensorPins) {
					GpioPinEventConfig config = new GpioPinEventConfig(pin, GpioPinMode.Input, GpioPinEventStates.ALL);
					GpioPollingManager.RegisterGpioEvent(config);
					RegisterEventDelegate(pin, OnSoundSensorValueChanged);
				}
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

			if (!Core.Config.IRSensorPins.Contains(e.PinNumber)) {
				return;
			}

			switch (e.PinState) {
				case GpioPinState.On:
					Logger.Log($"An Object is in front of the sensor! Pin -> {e.PinNumber}");

					if (Core.PiController.EnableExperimentalFunction && Core.Config.RelayPins[0] != 0) {
						SetGpioValue(Core.Config.RelayPins[0], GpioPinMode.Output, GpioPinState.On);
					}

					break;

				case GpioPinState.Off:
					Logger.Log($"No objects detected! Pin -> {e.PinNumber}");

					if (Core.PiController.EnableExperimentalFunction && Core.Config.RelayPins[0] != 0) {
						SetGpioValue(Core.Config.RelayPins[0], GpioPinMode.Output, GpioPinState.Off);
					}

					break;

				default:
					break;
			}
		}

		private void OnRelayPinValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			if (e == null || sender == null) {
				return;
			}

			if (!Core.Config.RelayPins.Contains(e.PinNumber)) {
				return;
			}

			switch (e.PinState) {
				case GpioPinState.On:
					Logger.Log($"Relay module connected to {e.PinNumber} gpio pin set to ON state. (OFF)", Enums.LogLevels.Info);
					break;

				case GpioPinState.Off:
					Logger.Log($"Relay module connected to {e.PinNumber} gpio pin set to OFF state. (ON)", Enums.LogLevels.Info);
					break;

				default:
					break;
			}
		}

		private void OnSoundSensorValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			if (e == null || sender == null) {
				return;
			}

			if (!Core.Config.SoundSensorPins.Contains(e.PinNumber)) {
				return;
			}

			switch (e.PinState) {
				case GpioPinState.On:
					Logger.Log($"Sound dectected! Pin -> {e.PinNumber}");
					break;

				case GpioPinState.Off:
					Logger.Log($"No sound. Pin -> {e.PinNumber}", LogLevels.Trace);
					break;

				default:
					break;
			}
		}

		public bool IsDriverProperlyInitialized => GetDriver().IsDriverProperlyInitialized;

		private IGpioControllerDriver GetDriver() {
			switch (CurrentGpioDriver) {
				case EGpioDriver.RaspberryIODriver:
					if (GpioControllerDriver != null && GpioControllerDriver.IsDriverProperlyInitialized) {
						return GpioControllerDriver;
					}
					break;
				default:
					throw new InvalidOperationException("Internal error with the drivers.");
			}

			return GpioControllerDriver;
		}

		public GpioPinConfig GetGpioConfig(int pinNumber) => GetDriver().GetGpioConfig(pinNumber);

		public bool SetGpioValue(int pin, GpioPinMode mode) => GetDriver().SetGpioValue(pin, mode);

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) => GetDriver().SetGpioValue(pin, mode, state);

		public bool SetGpioValue(int pin, GpioPinState state) => GetDriver().SetGpioValue(pin, state);

		public bool SetGpioWithTimeout(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration) => GetDriver().SetGpioWithTimeout(pin, mode, state, duration);

		public void ShutdownDriver() => GetDriver().ShutdownDriver();

		public async Task<bool> RelayTestServiceAsync(GpioCycles selectedCycle, int singleChannelValue = 0) => await GetDriver().RelayTestServiceAsync(selectedCycle, singleChannelValue).ConfigureAwait(false);

		public void UpdatePinConfig(int pin, GpioPinMode mode, GpioPinState value, TimeSpan duration) => GetDriver().UpdatePinConfig(pin, mode, value, duration);

		public GpioPinState GpioPinStateRead(int pin) => GetDriver().GpioPinStateRead(pin);

		public bool GpioDigitalRead(int pin) => GetDriver().GpioDigitalRead(pin);

		public int GpioPhysicalPinNumber(int bcmPin) => GetDriver().GpioPhysicalPinNumber(bcmPin);
	}
}
