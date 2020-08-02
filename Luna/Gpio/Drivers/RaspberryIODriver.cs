using Luna.Gpio.Config;
using Luna.Gpio.Controllers;
using Luna.Gpio.Exceptions;
using Luna.Logging;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.Drivers {
	internal class RaspberryIODriver : GpioControllerDriver {
		internal RaspberryIODriver(InternalLogger logger, PinsWrapper pins, PinConfig pinConfig, NumberingScheme scheme) : base(logger, pins, GpioDriver.RaspberryIODriver, pinConfig, scheme) { }

		internal override GpioControllerDriver Init() {
			if (!GpioCore.IsAllowedToExecute) {
				throw new DriverInitializationFailedException(nameof(RaspberryIODriver), "Not allowed to initialize.");
			}

			Pi.Init<BootstrapWiringPi>();
			return this;
		}

		internal override Pin GetPinConfig(int pinNumber) {
			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return new Pin();
			}

			if (!PinController.IsValidPin(pinNumber)) {
				return new Pin();
			}

			GpioPin pin = (GpioPin) Pi.Gpio[pinNumber];
			return new Pin(pinNumber, (GpioPinState) pin.ReadValue(), (GpioPinMode) pin.PinMode);
		}

		internal override bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
			GpioPin.PinMode = (GpioPinDriveMode) mode;
			return true;
		}

		internal override bool SetGpioValue(int pin, GpioPinState state) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];

			if (GpioPin.PinMode != GpioPinDriveMode.Output) {
				return false;
			}

			GpioPin.Write((GpioPinValue) state);
			return true;
		}

		internal override bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
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

		internal override GpioPinState GpioPinStateRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return GpioPinState.Off;
			}

			GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
			return (GpioPinState) gpioPin.ReadValue();
		}

		internal override bool GpioDigitalRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			return ((GpioPin) Pi.Gpio[pin]).Read();
		}

		internal override int GpioPhysicalPinNumber(int bcmPin) {
			if (!PinController.IsValidPin(bcmPin)) {
				return -1;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[bcmPin];
			return GpioPin.PhysicalPinNumber;
		}
	}
}
