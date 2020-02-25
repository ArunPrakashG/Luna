using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	internal class RaspberryIODriver : IGpioControllerDriver {
		public bool IsDriverProperlyInitialized { get; private set; }
		public PinConfig PinConfig => PinConfigManager.GetConfiguration();
		public Enums.EGPIO_DRIVERS DriverName => Enums.EGPIO_DRIVERS.RaspberryIODriver;

		public NumberingScheme NumberingScheme { get; set; }

		public IGpioControllerDriver InitDriver(NumberingScheme scheme) {
			if (!PiGpioController.IsAllowedToExecute) {
				CastDriver<IGpioControllerDriver>(this)?.Logger.Warning("Failed to initialize Gpio Controller Driver. (Driver isn't allowed to execute.)");
				IsDriverProperlyInitialized = false;
				return this;
			}

			NumberingScheme = scheme;
			Pi.Init<BootstrapWiringPi>();
			IsDriverProperlyInitialized = true;
			return this;
		}

		public Pin? GetPinConfig(int pinNumber) {
			if (!PiGpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return null;
			}

			if (!PinController.IsValidPin(pinNumber)) {
				CastDriver<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return null;
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
			CastDriver<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.");
			CastDriver<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, mode));
			return true;
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];

			if (GpioPin.PinMode == GpioPinDriveMode.Output) {
				GpioPin.Write((GpioPinValue) state);
				CastDriver<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state.");
				CastDriver<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, state));
				return true;
			}

			return false;
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!PinController.IsValidPin(pin) || !IsDriverProperlyInitialized) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
			GpioPin.PinMode = (GpioPinDriveMode) mode;

			if (mode == GpioPinMode.Output) {
				GpioPin.Write((GpioPinValue) state);
				CastDriver<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state with ({mode.ToString()}) mode.");
				CastDriver<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, state, mode));
				return true;
			}

			CastDriver<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.");
			CastDriver<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, mode));
			return true;
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!PinController.IsValidPin(pin)) {
				CastDriver<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return GpioPinState.Off;
			}

			GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
			return (GpioPinState) gpioPin.ReadValue();
		}

		public bool GpioDigitalRead(int pin) {
			if (!PinController.IsValidPin(pin)) {
				CastDriver<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
			return gpioPin.Read();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!PinController.IsValidPin(bcmPin)) {
				CastDriver<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return 0;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[bcmPin];
			return GpioPin.PhysicalPinNumber;
		}

		public IGpioControllerDriver? CastDriver<T>(T driver) where T : IGpioControllerDriver {
			if (driver == null) {
				return null;
			}

			return driver;
		}
	}
}
