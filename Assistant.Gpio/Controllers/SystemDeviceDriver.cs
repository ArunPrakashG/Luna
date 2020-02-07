using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Threading.Tasks;
using static Assistant.Gpio.PiController;

namespace Assistant.Gpio.Controllers {
	internal class SystemDeviceDriver : IGpioControllerDriver {
		private readonly ILogger Logger;
		private GpioController? Controller { get; set; }

		public bool IsDriverProperlyInitialized { get; private set; }

		internal SystemDeviceDriver(GpioPinController gpioController) {			
			Logger = gpioController.Logger;
		}

		internal SystemDeviceDriver? InitDriver(PinNumberingScheme numberingScheme) {
			if (!PiController.IsAllowedToExecute) {
				Logger.Warning("Failed to initialize Gpio Controller Driver.");
				IsDriverProperlyInitialized = false;
				return null;
			}

			Controller = new GpioController(numberingScheme);
			IsDriverProperlyInitialized = true;
			return this;
		}


		public GpioPinConfig GetGpioConfig(int pinNumber) {
			if (!PiController.IsValidPin(pinNumber) || Controller == null) {
				return new GpioPinConfig();
			}

			PinValue value = Controller.Read(pinNumber);
			PinMode mode = Controller.GetPinMode(pinNumber);
			GpioPinConfig config = new GpioPinConfig(pinNumber, value == PinValue.High ? GpioPinState.Off : GpioPinState.On, mode == PinMode.Input ? GpioPinMode.Input : GpioPinMode.Output, false, 0);
			return config;
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

		public Task<bool> RelayTestServiceAsync(IEnumerable<int> pins, GpioCycles selectedCycle, int singleChannelValue = 0) {
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
