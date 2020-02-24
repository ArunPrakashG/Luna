using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using System;
using System.Device.Gpio;
using static Assistant.Gpio.Config.PinConfig;
using static Assistant.Gpio.Controllers.PiController;

namespace Assistant.Gpio.Drivers {
	internal class SystemDeviceDriver : IGpioControllerDriver {
		private GpioController? Controller { get; set; }

		public bool IsDriverProperlyInitialized { get; private set; }

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		internal SystemDeviceDriver InitDriver(PinNumberingScheme numberingScheme) {
			if (!PiController.IsAllowedToExecute) {
				Logger.Warning("Failed to initialize Gpio Controller Driver.");
				IsDriverProperlyInitialized = false;
				return null;
			}

			Controller = new GpioController(numberingScheme);
			IsDriverProperlyInitialized = true;
			return this;
		}


		public Pin GetPinConfig(int pinNumber) {
			if (!PiController.IsValidPin(pinNumber) || Controller == null) {
				return null;
			}

			PinValue value = Controller.Read(pinNumber);
			PinMode mode = Controller.GetPinMode(pinNumber);
			Pin config = new Pin(pinNumber, value == PinValue.High ? GpioPinState.Off : GpioPinState.On, mode == PinMode.Input ? GpioPinMode.Input : GpioPinMode.Output);
			return config;
		}

		public IGpioControllerDriver? CastDriver<T>(T driver) where T : IGpioControllerDriver {
			return driver;
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			throw new NotImplementedException();
		}

		public GpioPinState GpioPinStateRead(int pin) {
			throw new NotImplementedException();
		}

		public bool GpioDigitalRead(int pin) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			throw new NotImplementedException();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			throw new NotImplementedException();
		}
	}
}
