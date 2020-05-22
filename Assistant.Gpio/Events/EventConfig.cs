using Assistant.Gpio.Events.EventArgs;
using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	internal struct EventConfig {
		internal readonly int GpioPin;

		internal readonly GpioPinMode PinMode;

		internal readonly PinEventStates PinEventState;

		internal readonly Func<OnValueChangedEventArgs, bool> OnEvent;

		internal bool IsEventRegistered { get; private set; }

		internal EventConfig(int _gpioPin, GpioPinMode _pinMode, PinEventStates _pinEventState, Func<OnValueChangedEventArgs, bool> _onEvent) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
			IsEventRegistered = false;
			OnEvent = _onEvent;
		}

		internal void SetEventRegisteredStatus(bool _isRegistered) => IsEventRegistered = _isRegistered;
	}
}
