using Luna.ExternalExtensions;
using Luna.Gpio.Config;
using Luna.Gpio.Controllers;
using Luna.Gpio.Drivers;
using Luna.Gpio.Events.EventArgs;
using Luna.Gpio.Exceptions;
using Luna.Logging.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;
using static Luna.Logging.Enums;

namespace Luna.Gpio.Events {
	internal class Generator {
		private const int POLL_DELAY = 1; // in ms		
		private readonly ILogger Logger;
		private readonly GpioCore Core;
		private readonly GeneratedValue PreviousValue = new GeneratedValue();
		private readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private readonly GpioControllerDriver Driver;

		private bool OverridePolling;
		private bool IsPossible => !Config.IsEventRegistered && Driver != null && Driver.IsDriverInitialized;

		internal readonly EventConfig Config;

		internal Generator(GpioCore _core, EventConfig _config, ILogger _logger) {
			Logger = _logger;
			Core = _core;
			Config = _config;
			Driver = PinController.GetDriver() ?? throw new DriverNotInitializedException();
			Init();
		}

		internal void OverrideEventPolling() => OverridePolling = true;

		private void Init() {
			if (Driver == null) {
				throw new DriverNotInitializedException();
			}

			if (!IsPossible) {
				Logger.Log("An error occurred. Check if the specified pin is valid.", LogLevels.Warn);
				return;
			}

			if (Config.PinMode == GpioPinMode.Alt01 || Config.PinMode == GpioPinMode.Alt02) {
				Logger.Log("Currently only Output/Input polling is supported.", LogLevels.Warn);
				return;
			}

			if (!Driver.SetGpioValue(Config.GpioPin, Config.PinMode)) {
				Logger.Error("Failed to set pin value.");
				return;
			}

			SetInitalValue();
			Helpers.InBackgroundThread(async () => await PollAsync().ConfigureAwait(false), true);
		}

		private async Task PollAsync() {
			if (!IsPossible) {
				Logger.Log("An error occurred. Check if the specified pin is valid.", LogLevels.Warn);
				return;
			}

			Config.SetEventRegisteredStatus(true);
			await Sync.WaitAsync().ConfigureAwait(false);
			Logger.Log($"Started '{(Config.PinMode == GpioPinMode.Input ? "Input" : "Output")}' pin polling for {Config.GpioPin}.", LogLevels.Trace);

			try {
				do {
					bool currentValue = Driver.GpioDigitalRead(Config.GpioPin);
					GpioPinState currentState = currentValue ? GpioPinState.Off : GpioPinState.On;
					OnPollResult(currentValue, currentState);
					await Task.Delay(POLL_DELAY);
				} while (!OverridePolling);
			}
			finally {
				Config.SetEventRegisteredStatus(false);
				Logger.Log($"Polling for '{Config.GpioPin}' has been stopped.", LogLevels.Trace);
				Sync.Release();
			}
		}

		private void OnPollResult(bool currentValue, GpioPinState currentState) {
			if (!IsPossible) {
				return;
			}

			bool isSame = PreviousValue.PinState == currentState;
			Pin pinConfig = Driver.GetPinConfig(Config.GpioPin);
			OnValueChangedEventArgs args;

			switch (Config.PinEventState) {
				case PinEventStates.Activated when currentState == GpioPinState.On && !isSame:
					args = new OnValueChangedEventArgs(Config.GpioPin, currentState, currentValue,
					Config.PinMode, PinEventStates.Activated, PreviousValue.PinState, PreviousValue.DigitalValue);
					Config.OnEvent?.Invoke(args);
					break;

				case PinEventStates.Deactivated when currentState == GpioPinState.Off && !isSame:
					args = new OnValueChangedEventArgs(Config.GpioPin, currentState, currentValue,
					Config.PinMode, PinEventStates.Deactivated, PreviousValue.PinState, PreviousValue.DigitalValue);
					Config.OnEvent?.Invoke(args);
					break;
				case PinEventStates.Both when PreviousValue.PinState != currentState:
					args = new OnValueChangedEventArgs(Config.GpioPin, currentState, currentValue,
					Config.PinMode, PinEventStates.Both, PreviousValue.PinState, PreviousValue.DigitalValue);
					Config.OnEvent?.Invoke(args);
					break;
			}

			PreviousValue.Set(currentState, currentValue);
		}

		private void SetInitalValue() {
			if (!IsPossible) {
				Logger.Log("An error occurred. Check if the specified pin is valid.", LogLevels.Warn);
				return;
			}

			if (Config.PinMode == GpioPinMode.Output) {
				Driver.SetGpioValue(Config.GpioPin, GpioPinState.Off);
			}

			PreviousValue.Set(GpioPinState.Off, true);
			Logger.Trace($"Initial pin event values has been set for {Config.GpioPin} pin.");
		}
	}
}
