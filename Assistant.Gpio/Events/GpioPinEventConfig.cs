using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	public struct GpioPinEventConfig {
		public readonly int GpioPin;

		public readonly GpioPinMode PinMode;

		public readonly GpioPinEventStates PinEventState;

		public GpioPinEventConfig(int _gpioPin, GpioPinMode _pinMode, GpioPinEventStates _pinEventState) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
		}
	}
}
