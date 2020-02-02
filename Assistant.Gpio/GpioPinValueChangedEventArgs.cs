using static Assistant.Gpio.PiController;

namespace Assistant.Gpio {
	public class GpioPinValueChangedEventArgs {
		public int PinNumber { get; set; }

		public GpioPinState PinState { get; set; }

		public GpioPinState PinPreviousState { get; set; }

		public bool PinCurrentDigitalValue { get; set; }

		public bool PinPreviousDigitalValue { get; set; }

		public GpioPinMode PinDriveMode { get; set; }

		public int GpioPhysicalPinNumber { get; set; }

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
