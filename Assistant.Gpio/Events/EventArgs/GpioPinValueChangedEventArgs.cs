using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events.EventArgs {
	public class GpioPinValueChangedEventArgs {
		public readonly int PinNumber;

		public readonly GpioPinState PinState;

		public readonly GpioPinState PinPreviousState;

		public readonly bool PinCurrentDigitalValue;

		public readonly bool PinPreviousDigitalValue;

		public readonly GpioPinMode PinDriveMode;

		public readonly int GpioPhysicalPinNumber;

		public GpioPinValueChangedEventArgs(int _pinNumber, GpioPinState _pinState,
			GpioPinState _pinPreviousState, bool _pinCurrentDigitalValue, bool _pinPreviousDigitalValue,
			GpioPinMode _pinDriveMode, int _gpioPhysicalPinNumber) {
			PinNumber = _pinNumber;
			PinState = _pinState;
			PinPreviousState = _pinPreviousState;
			PinCurrentDigitalValue = _pinCurrentDigitalValue;
			PinPreviousDigitalValue = _pinPreviousDigitalValue;
			PinDriveMode = _pinDriveMode;
			GpioPhysicalPinNumber = _gpioPhysicalPinNumber;
		}
	}
}
