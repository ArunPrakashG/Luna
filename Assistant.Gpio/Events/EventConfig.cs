using Assistant.Gpio.Events.EventArgs;
using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	public struct EventConfig {
		public readonly int GpioPin;

		public readonly GpioPinMode PinMode;

		public readonly GpioPinEventStates PinEventState;

		public Action<object, OnValueChangedEventArgs> OnFireAction { get; private set; }

		public EventConfig(int _gpioPin, GpioPinMode _pinMode, GpioPinEventStates _pinEventState, Action<object, OnValueChangedEventArgs> _func) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
			OnFireAction = _func;
		}

		public void SetEventAction(Action<object, OnValueChangedEventArgs> action) {
			if(action == null) {
				return;
			}

			OnFireAction = action;
		}
	}
}
