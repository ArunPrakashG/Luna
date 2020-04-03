using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	internal struct EventConfig {
		internal readonly int GpioPin;

		internal readonly GpioPinMode PinMode;

		internal readonly GpioPinEventStates PinEventState;

		internal readonly SensorType Type;

		internal bool IsEventRegistered { get; private set; }

		internal EventConfig(int _gpioPin, GpioPinMode _pinMode, GpioPinEventStates _pinEventState, SensorType _type) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
			Type = _type;
			IsEventRegistered = false;
		}

		internal void SetEventRegisteredStatus(bool _isRegistered) {
			IsEventRegistered = _isRegistered;
		}
	}
}
