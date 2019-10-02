using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assistant.AssistantCore.PiGpio.GpioControllers {
	internal class SystemDeviceDriver : IGpioControllerDriver {
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

		public Enums.GpioPinState GpioPinStateRead(int pin) {
			throw new NotImplementedException();
		}

		public Task<bool> RelayTestServiceAsync(Enums.GpioCycles selectedCycle, int singleChannelValue = 0) {
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

		public bool SetGpioWithTimeout(int pin, Enums.GpioPinMode mode, Enums.GpioPinState state, TimeSpan duration) {
			throw new NotImplementedException();
		}

		public void ShutdownDriver() {
			throw new NotImplementedException();
		}

		public void UpdatePinConfig(int pin, Enums.GpioPinMode mode, Enums.GpioPinState value, TimeSpan duration) {
			throw new NotImplementedException();
		}
	}
}
