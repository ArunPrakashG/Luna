using Assistant.Gpio.Events.EventArgs;
using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	public struct EventConfig {
		public readonly int GpioPin;

		public readonly GpioPinMode PinMode;

		public readonly GpioPinEventStates PinEventState;

		public readonly Enums.SensorType Type;

		public bool IsEventRegistered { get; private set; }

		public EventConfig(int _gpioPin, GpioPinMode _pinMode, GpioPinEventStates _pinEventState, SensorType _type) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
			Type = _type;
			IsEventRegistered = false;
		}

		public void SetEventRegisteredStatus(bool _isRegistered) {
			IsEventRegistered = _isRegistered;
		}
	}
}
