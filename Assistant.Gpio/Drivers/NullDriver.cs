using Assistant.Gpio.Config;
using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	public class NullDriver : IGpioControllerDriver {
		public bool IsDriverProperlyInitialized => throw new NotImplementedException();

		public NumberingScheme NumberingScheme { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public EGPIO_DRIVERS DriverName => throw new NotImplementedException();

		public PinConfig PinConfig => throw new NotImplementedException();

		public IGpioControllerDriver? CastDriver<T>(T driver) where T : IGpioControllerDriver {
			throw new NotImplementedException();
		}

		public Pin? GetPinConfig(int pinNumber) {
			throw new NotImplementedException();
		}

		public bool GpioDigitalRead(int pin) {
			throw new NotImplementedException();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			throw new NotImplementedException();
		}

		public GpioPinState GpioPinStateRead(int pin) {
			throw new NotImplementedException();
		}

		public IGpioControllerDriver InitDriver(NumberingScheme scheme) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			throw new NotImplementedException();
		}
	}
}
