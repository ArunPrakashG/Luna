using Assistant.Gpio.Events.EventArgs;
using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	public struct EventConfig {
		public readonly int GpioPin;

		public readonly GpioPinMode PinMode;

		public readonly GpioPinEventStates PinEventState;

		public readonly Func<object, OnValueChangedEventArgs, bool> Function;

		public EventConfig(int _gpioPin, GpioPinMode _pinMode, GpioPinEventStates _pinEventState, Func<object, OnValueChangedEventArgs, bool> _func) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
			Function = _func;
		}
	}
}
