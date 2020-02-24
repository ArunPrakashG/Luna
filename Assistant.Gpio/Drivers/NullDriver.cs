using Assistant.Gpio.Config;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Assistant.Gpio.Config.PinConfig;
using static Assistant.Gpio.Controllers.PiController;

namespace Assistant.Gpio.Drivers {
	public class NullDriver : IGpioControllerDriver {
		public bool IsDriverProperlyInitialized => throw new NotImplementedException();

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

		public GpioPinState GpioPinStateRead(int pin) {
			throw new NotImplementedException();
		}

		public Task<bool> RelayTestAsync(IEnumerable<int> relayPins, GpioCycles selectedCycle, int singleChannelValue = 0) {
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

		public bool SetGpioWithTimeout(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration) {
			throw new NotImplementedException();
		}

		public void ShutdownDriver() {
			throw new NotImplementedException();
		}

		public void UpdatePinConfig(int pin, GpioPinMode mode, GpioPinState value, TimeSpan duration) {
			throw new NotImplementedException();
		}
	}
}
