using Assistant.Gpio.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Gpio.Drivers {
	internal class WiringPiDriver : IGpioControllerDriver {
		public bool IsDriverProperlyInitialized => throw new NotImplementedException();

		public Enums.NumberingScheme NumberingScheme { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public Enums.EGPIO_DRIVERS DriverName => Enums.EGPIO_DRIVERS.WiringPiDriver;

		public PinConfig PinConfig => throw new NotImplementedException();

		public IGpioControllerDriver? CastDriver<T>(T driver) where T : IGpioControllerDriver {
			throw new NotImplementedException();
		}

		public Pin GetPinConfig(int pinNumber) {
			throw new NotImplementedException();
		}

		public bool GpioDigitalRead(int pin) {
			throw new NotImplementedException();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			throw new NotImplementedException();
		}

		public Enums.GpioPinState GpioPinStateRead(int pin) {
			throw new NotImplementedException();
		}

		public IGpioControllerDriver InitDriver(Enums.NumberingScheme scheme) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode, Enums.GpioPinState state) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, Enums.GpioPinState state) {
			throw new NotImplementedException();
		}
	}
}
