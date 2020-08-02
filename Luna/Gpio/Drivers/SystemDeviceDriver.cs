using Luna.Gpio.Config;
using Luna.Gpio.Controllers;
using Luna.Gpio.Exceptions;
using Luna.Logging.Interfaces;
using System;
using System.Device.Gpio;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.Drivers {
	public class SystemDeviceDriver : GpioControllerDriver {
		public ILogger Logger { get; private set; }

		public PinsWrapper AvailablePins { get; private set; }

		private GpioController DriverController;

		public bool IsDriverInitialized { get; private set; }
		public Enums.GpioDriver DriverName => Enums.GpioDriver.SystemDevicesDriver;

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		public NumberingScheme NumberingScheme { get; private set; }

		public GpioControllerDriver InitDriver(ILogger _logger, PinsWrapper _availablePins, NumberingScheme _scheme) {
			Logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			AvailablePins = _availablePins;

			if (!GpioCore.IsAllowedToExecute) {
				IsDriverInitialized = false;
				throw new DriverInitializationFailedException(nameof(RaspberryIODriver), "Not allowed to initialize.");
			}

			NumberingScheme = _scheme;
			DriverController = new GpioController((PinNumberingScheme) _scheme);
			IsDriverInitialized = true;
			return this;
		}

		private void ClosePin(int pinNumber) {
			if(DriverController == null) {
				return;
			}

			if (PinController.IsValidPin(pinNumber) && DriverController.IsPinOpen(pinNumber)) {
				DriverController.ClosePin(pinNumber);
			}
		}

		public Pin GetPinConfig(int pinNumber) {
			if (!PinController.IsValidPin(pinNumber) || DriverController == null || !IsDriverInitialized) {
				return new Pin();
			}

			if (DriverController == null) {
				return new Pin();
			}

			try {
				if (!DriverController.IsPinOpen(pinNumber)) {
					DriverController.OpenPin(pinNumber);
				}

				if (!DriverController.IsPinOpen(pinNumber)) {
					return new Pin();
				}

				PinValue value = DriverController.Read(pinNumber);
				PinMode mode = DriverController.GetPinMode(pinNumber);
				Pin config = new Pin(pinNumber, value == PinValue.High ? GpioPinState.Off : GpioPinState.On, mode == PinMode.Input ? GpioPinMode.Input : GpioPinMode.Output);
				return config;
			}
			finally {
				ClosePin(pinNumber);
			}
		}

		public GpioControllerDriver Cast<T>(T driver) where T : GpioControllerDriver => driver;

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			try {
				if (DriverController == null) {
					return false;
				}

				if (!DriverController.IsPinModeSupported(pin, (PinMode) mode)) {
					return false;
				}

				if (!DriverController.IsPinOpen(pin)) {
					DriverController.OpenPin(pin);
				}

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				DriverController.SetPinMode(pin, (PinMode) mode);
				return true;
			}
			finally {
				ClosePin(pin);
			}
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			try {
				if (DriverController == null) {
					return false;
				}

				if (!DriverController.IsPinModeSupported(pin, (PinMode) mode)) {
					return false;
				}

				if (!DriverController.IsPinOpen(pin)) {
					DriverController.OpenPin(pin);
				}

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				DriverController.SetPinMode(pin, (PinMode) mode);
				DriverController.Write(pin, state == GpioPinState.Off ? PinValue.High : PinValue.Low);
				return true;
			}
			finally {
				ClosePin(pin);
			}
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return GpioPinState.Off;
			}

			try {
				if (DriverController == null) {
					return GpioPinState.Off;
				}

				if (!DriverController.IsPinOpen(pin)) {
					DriverController.OpenPin(pin);
				}

				if (!DriverController.IsPinOpen(pin)) {
					return GpioPinState.Off;
				}

				return DriverController.Read(pin) == PinValue.High ? GpioPinState.Off : GpioPinState.On;
			}
			finally {
				ClosePin(pin);
			}
		}

		public bool GpioDigitalRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			try {
				if (DriverController == null) {
					return false;
				}

				if (!DriverController.IsPinOpen(pin)) {
					DriverController.OpenPin(pin);
				}

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				return !(DriverController.Read(pin) == PinValue.High);
			}
			finally {
				ClosePin(pin);
			}
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			try {
				if (DriverController == null) {
					return false;
				}

				if (!DriverController.IsPinOpen(pin)) {
					DriverController.OpenPin(pin);
				}

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				DriverController.Write(pin, state == GpioPinState.Off ? PinValue.High : PinValue.Low);				
				return true;
			}
			finally {
				ClosePin(pin);
			}
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!PinController.IsValidPin(bcmPin) || !IsDriverInitialized) {
				return -1;
			}

			try {
				if (DriverController == null) {
					return -1;
				}

				if (!DriverController.IsPinOpen(bcmPin)) {
					DriverController.OpenPin(bcmPin);
				}

				if (!DriverController.IsPinOpen(bcmPin)) {
					return -1;
				}

				return -1;
			}
			finally {
				ClosePin(bcmPin);
			}
		}
	}
}
