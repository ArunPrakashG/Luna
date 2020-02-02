using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Assistant.Gpio.PiController;

namespace Assistant.Gpio.Controllers {
	public class NullDriver : IGpioControllerDriver {
		public bool IsDriverProperlyInitialized => throw new NotImplementedException();

		public GpioPinConfig GetGpioConfig(int pinNumber) {
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

		public Task<bool> RelayTestServiceAsync(IEnumerable<int> relayPins, GpioCycles selectedCycle, int singleChannelValue = 0) {
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
