using Assistant.Gpio.Controllers;
using Assistant.Gpio.Events.EventArgs;
using Assistant.Logging.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Gpio.Controllers.PiController;
using static Assistant.Logging.Enums;

namespace Assistant.Gpio.Events {
	public sealed class GpioEventGenerator {
		private readonly PiController PiController;
		private readonly GpioPinController Controller;
		private readonly ILogger Logger;

		private GpioEventManager EventManager { get; set; }
		private bool OverrideEventWatcher { get; set; }
		public GpioPinEventConfig EventPinConfig { get; private set; } = new GpioPinEventConfig();
		public bool IsEventRegistered { get; private set; } = false;

		public Thread? PollingThread { get; private set; }

		public (object sender, GpioPinValueChangedEventArgs e) _GpioPinValueChanged { get; private set; }

		public delegate void GpioPinValueChangedEventHandler(object sender, GpioPinValueChangedEventArgs e);

		public event GpioPinValueChangedEventHandler? GpioPinValueChanged;

		private (object sender, GpioPinValueChangedEventArgs e) _GpioPinValue {
			get => _GpioPinValueChanged;
			set {
				GpioPinValueChanged?.Invoke(value.sender, value.e);
				_GpioPinValueChanged = _GpioPinValue;
			}
		}

		public GpioEventGenerator(PiController piController, GpioPinController controller, GpioEventManager manager) {
			PiController = piController ?? throw new ArgumentNullException(nameof(manager), "The pi controller instance cannot be null!");
			Controller = controller ?? throw new ArgumentNullException(nameof(manager), "The controller instance cannot be null!");
			EventManager = manager ?? throw new ArgumentNullException(nameof(manager), "The event manager instance cannot be null!");
			Logger = EventManager.Logger;
		}

		public GpioEventGenerator InitEventGenerator() {
			if (PiController == null) {
				throw new InvalidOperationException("The gpio controller is probably malfunctioning.");
			}

			if (Controller == null) {
				throw new InvalidOperationException("The pin controller is probably malfunctioning.");
			}

			if (!Controller.IsDriverProperlyInitialized) {
				throw new InvalidOperationException("The pin controller isn't properly initialized.");
			}

			return this;
		}

		public void OverridePinPolling() => OverrideEventWatcher = true;

		private void StartPolling() {
			if (PiController == null) {
				Logger.Log("PiController is null. Polling failed.", LogLevels.Warn);
				return;
			}

			if (Controller == null) {
				Logger.Log("Controller is null. Polling failed.", LogLevels.Warn);
				return;
			}

			if (IsEventRegistered) {
				Logger.Log("There already seems to have an event registered on this instance.", LogLevels.Warn);
				return;
			}

			if (EventPinConfig.PinMode == GpioPinMode.Alt01 || EventPinConfig.PinMode == GpioPinMode.Alt02) {
				Logger.Log("Currently only Output/Input polling is supported.", LogLevels.Warn);
				return;
			}

			if (!Controller.SetGpioValue(EventPinConfig.GpioPin, EventPinConfig.PinMode)) {
				throw new InvalidOperationException("Internal error occurred. Check if the pin specified is correct.");
			}

			switch (EventPinConfig.PinMode) {
				case GpioPinMode.Input:
					break;
				case GpioPinMode.Output:
					if (!Controller.SetGpioValue(EventPinConfig.GpioPin, GpioPinState.Off)) {
						throw new InvalidOperationException("Internal error occurred. Check if the pin specified is correct.");
					}
					break;
				case GpioPinMode.Alt02:
				case GpioPinMode.Alt01:
				default:
					throw new InvalidOperationException("Internal error. The pin mode seems to be invalid. (No modes other than Input/Output is currently supported)");
			}

			bool initialValue = Controller.GpioDigitalRead(EventPinConfig.GpioPin);
			GpioPinState initialPinState = initialValue ? GpioPinState.Off : GpioPinState.On;
			int physicalPinNumber = Controller.GpioPhysicalPinNumber(EventPinConfig.GpioPin);
			_GpioPinValueChanged = (this, new GpioPinValueChangedEventArgs(EventPinConfig.GpioPin, initialPinState, GpioPinState.Off, initialValue, true, EventPinConfig.PinMode, physicalPinNumber));

			Logger.Log($"Started input pin polling for {EventPinConfig.GpioPin}.", LogLevels.Trace);
			IsEventRegistered = true;

			GpioPinValueChangedEventArgs e;
			GpioPinState previousPinState = initialPinState;
			bool previousPinValue = initialValue;

			PollingThread = Extensions.Helpers.InBackgroundThread(async () => {
				while (!OverrideEventWatcher) {
					bool currentPinValue = Controller.GpioDigitalRead(EventPinConfig.GpioPin);
					GpioPinState currentPinState = currentPinValue ? GpioPinState.Off : GpioPinState.On;

					switch (EventPinConfig.PinEventState) {
						case GpioPinEventStates.OFF when currentPinState == GpioPinState.Off && previousPinState != currentPinState:
							e = new GpioPinValueChangedEventArgs(EventPinConfig.GpioPin, currentPinState, previousPinState, currentPinValue, previousPinValue, EventPinConfig.PinMode, physicalPinNumber);
							_GpioPinValue = (this, e);
							break;

						case GpioPinEventStates.ON when currentPinState == GpioPinState.On && previousPinState != currentPinState:
							e = new GpioPinValueChangedEventArgs(EventPinConfig.GpioPin, currentPinState, previousPinState, currentPinValue, previousPinValue, EventPinConfig.PinMode, physicalPinNumber);
							_GpioPinValue = (this, e);
							break;

						case GpioPinEventStates.ALL when previousPinState != currentPinState:
							e = new GpioPinValueChangedEventArgs(EventPinConfig.GpioPin, currentPinState, previousPinState, currentPinValue, previousPinValue, EventPinConfig.PinMode, physicalPinNumber);
							_GpioPinValue = (this, e);
							break;
						case GpioPinEventStates.NONE:
							OverrideEventWatcher = true;
							Logger.Log($"Stopping event polling for pin -> {EventPinConfig.GpioPin} ...", LogLevels.Trace);
							break;
						default:
							break;
					}

					previousPinState = currentPinState;
					previousPinValue = currentPinValue;
					await Task.Delay(1).ConfigureAwait(false);
				}

				Logger.Log($"Polling for {EventPinConfig.GpioPin} has been stopped.", LogLevels.Trace);
			}, $"Polling thread {EventPinConfig.GpioPin}", true);
		}

		public bool StartPinPolling(GpioPinEventConfig config) {
			if (config == null) {
				return false;
			}

			if (PiController == null) {
				Logger.Log("PiController is null. Polling failed.", LogLevels.Warn);
				return false;
			}

			if (!PiController.IsControllerProperlyInitialized || !Controller.IsDriverProperlyInitialized) {
				return false;
			}

			if (!PiController.IsValidPin(config.GpioPin)) {
				Logger.Log("The specified pin is invalid.", LogLevels.Warn);
				return false;
			}

			EventPinConfig = config;
			StartPolling();
			return IsEventRegistered;
		}
	}
}
