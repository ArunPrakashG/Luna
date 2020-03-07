using Assistant.Extensions;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Drivers;
using Assistant.Gpio.Events.EventArgs;
using Assistant.Logging.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Gpio.Enums;
using static Assistant.Logging.Enums;

namespace Assistant.Gpio.Events {
	internal class Generator {
		private const int POLL_DELAY = 1; // in ms
		private static IGpioControllerDriver? Driver => PinController.GetDriver();
		private static ILogger Logger => PinEvents.Logger;
		private bool OverrideEventWatcher;
		private GpioPinState _previousPinState = GpioPinState.Off;
		private bool _previousDigitalState = true;
		private readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		public readonly EventConfig Config;
		public bool IsEventRegistered { get; private set; } = false;

		internal Generator(EventConfig config) => Config = config;

		public void OverridePolling() => OverrideEventWatcher = true;

		public void Poll() => _ = Helpers.InBackgroundThread(async () => await PollAsync(), true);

		/// <summary>
		/// Thread blocking method to poll the specified gpio pin in the EventConfig of this instance.
		/// </summary>
		/// <returns></returns>
		private async Task PollAsync() {
			if (Driver == null || !Driver.IsDriverProperlyInitialized) {
				Logger.Log("Controller is null. Polling failed.", LogLevels.Warn);
				return;
			}

			if (IsEventRegistered) {
				Logger.Log("There already seems to have an event registered on this instance.", LogLevels.Warn);
				return;
			}

			if (Config.PinMode == GpioPinMode.Alt01 || Config.PinMode == GpioPinMode.Alt02) {
				Logger.Log("Currently only Output/Input polling is supported.", LogLevels.Warn);
				return;
			}

			if (!Driver.SetGpioValue(Config.GpioPin, Config.PinMode)) {
				Logger.Error("Internal error occurred. Check if the pin specified is correct.");
				return;
			}

			if (Config.PinMode == GpioPinMode.Output) {
				Driver.SetGpioValue(Config.GpioPin, GpioPinState.Off);
			}

			await SetInitalValue();
			await Sync.WaitAsync().ConfigureAwait(false);
			IsEventRegistered = true;
			Logger.Log($"Started input pin polling for {Config.GpioPin}.", LogLevels.Trace);

			do {
				bool currentDigitalValue = Driver.GpioDigitalRead(Config.GpioPin);
				GpioPinState currentPinState = currentDigitalValue ? GpioPinState.Off : GpioPinState.On;

				switch (Config.PinEventState) {
					case GpioPinEventStates.ON when currentPinState == GpioPinState.On && _previousPinState != currentPinState:
					case GpioPinEventStates.OFF when currentPinState == GpioPinState.Off && _previousPinState != currentPinState:
					case GpioPinEventStates.ALL when _previousPinState != currentPinState:
						OnValueChangedEventArgs eventArgs = new OnValueChangedEventArgs(Config.GpioPin, currentPinState, currentDigitalValue, Config.PinMode, _previousPinState, _previousDigitalState);
						Config.OnFireAction?.Invoke(this, eventArgs);
						break;
					case GpioPinEventStates.NONE:
						OverrideEventWatcher = true;
						Logger.Log($"Stopping event polling for pin -> {Config.GpioPin} ...", LogLevels.Trace);
						break;
					default:
						break;
				}

				_previousPinState = currentPinState;
				_previousDigitalState = currentDigitalValue;
				await Task.Delay(POLL_DELAY);
			} while (!OverrideEventWatcher);

			IsEventRegistered = false;
			Logger.Log($"Polling for {Config.GpioPin} has been stopped.", LogLevels.Trace);
		}

		private async Task SetInitalValue() {
			if (Driver == null || !Driver.IsDriverProperlyInitialized) {
				Logger.Log("Controller is null. Polling failed.", LogLevels.Warn);
				return;
			}

			if (IsEventRegistered) {
				Logger.Log("There already seems to have an event registered on this instance.", LogLevels.Warn);
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				_previousPinState = GpioPinState.Off;
				_previousDigitalState = true;
				Logger.Trace($"Initial pin event values has been set for {Config.GpioPin} pin.");
			}
			finally {
				Sync.Release();
			}
		}
	}
}
