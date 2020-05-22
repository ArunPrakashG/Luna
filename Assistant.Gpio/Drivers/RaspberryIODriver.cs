using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Exceptions;
using Assistant.Logging.Interfaces;
using System;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	public class RaspberryIODriver : IGpioControllerDriver {
		public ILogger Logger { get; private set; }

		public bool IsDriverInitialized { get; private set; }

		public AvailablePins AvailablePins { get; private set; }

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		public GpioDriver DriverName => GpioDriver.RaspberryIODriver;

		public NumberingScheme NumberingScheme { get; private set; }

		public IGpioControllerDriver InitDriver(ILogger _logger, AvailablePins _availablePins, NumberingScheme _scheme) {
			Logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			AvailablePins = _availablePins;

			if (!GpioCore.IsAllowedToExecute) {
				IsDriverInitialized = false;
				throw new DriverInitializationFailedException(nameof(RaspberryIODriver), "Not allowed to initialize.");
			}

			NumberingScheme = _scheme;
			Pi.Init<BootstrapWiringPi>();
			IsDriverInitialized = true;
			return this;
		}

		public Pin GetPinConfig(int pinNumber) {
			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return new Pin();
			}

			if (!PinController.IsValidPin(pinNumber)) {
				return new Pin();
			}

			GpioPin pin = (GpioPin) Pi.Gpio[pinNumber];
			return new Pin(pinNumber, (GpioPinState) pin.ReadValue(), (GpioPinMode) pin.PinMode);
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
			GpioPin.PinMode = (GpioPinDriveMode) mode;
			return true;
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];

			if(GpioPin.PinMode != GpioPinDriveMode.Output) {
				return false;
			}

			GpioPin.Write((GpioPinValue) state);
			return true;
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
			GpioPin.PinMode = (GpioPinDriveMode) mode;

			if (mode == GpioPinMode.Output) {
				GpioPin.Write((GpioPinValue) state);
				return true;
			}

			return true;
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return GpioPinState.Off;
			}

			GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
			return (GpioPinState) gpioPin.ReadValue();
		}

		public bool GpioDigitalRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}
			
			return ((GpioPin) Pi.Gpio[pin]).Read();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!PinController.IsValidPin(bcmPin)) {
				return -1;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[bcmPin];
			return GpioPin.PhysicalPinNumber;
		}

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver => driver;
	}
}
