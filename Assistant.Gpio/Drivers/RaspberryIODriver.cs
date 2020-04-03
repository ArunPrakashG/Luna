using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Assistant.Logging.Interfaces;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	public class RaspberryIODriver : IGpioControllerDriver {
		private ILogger? Logger;
		public bool IsDriverInitialized { get; private set; }
		public PinConfig PinConfig => PinConfigManager.GetConfiguration();
		public GpioDriver DriverName => GpioDriver.RaspberryIODriver;

		public NumberingScheme NumberingScheme { get; set; }

		public IGpioControllerDriver InitDriver(NumberingScheme scheme) {
			Logger = Cast((IGpioControllerDriver) this)?.Logger;

			if (!Controllers.GpioController.IsAllowedToExecute) {
				Logger?.Warning($"Failed to initialize Gpio Controller Driver. (Driver isn't allowed to execute.) ({DriverName})");
				IsDriverInitialized = false;
				return this;
			}

			NumberingScheme = scheme;
			Pi.Init<BootstrapWiringPi>();
			IsDriverInitialized = true;
			return this;
		}

		public Pin GetPinConfig(int pinNumber) {
			if (!Controllers.GpioController.IsAllowedToExecute || !IsDriverInitialized) {
				return new Pin();
			}

			if (!PinController.IsValidPin(pinNumber)) {
				Logger?.Log("The specified pin is invalid.");
				return new Pin();
			}

			GpioPin pin = (GpioPin) Pi.Gpio[pinNumber];
			return new Pin(pinNumber, (GpioPinState) pin.ReadValue(), (GpioPinMode) pin.PinMode);
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			try {
				GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
				GpioPin.PinMode = (GpioPinDriveMode) mode;
				Logger?.Trace($"Configured ({pin}) gpio pin with ({mode}) mode.");
				Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, mode));
				return true;
			}
			finally {
				Cast<IGpioControllerDriver>(this).UpdatePinConfig(GetPinConfig(pin));
			}
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			try {
				GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];

				if (GpioPin.PinMode == GpioPinDriveMode.Output) {
					GpioPin.Write((GpioPinValue) state);
					Logger?.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state.");
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(new Pin(pin, state));
					return true;
				}

				return false;
			}
			finally {
				Cast<IGpioControllerDriver>(this).UpdatePinConfig(GetPinConfig(pin));
			}
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				return false;
			}

			try {
				GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
				GpioPin.PinMode = (GpioPinDriveMode) mode;

				if (mode == GpioPinMode.Output) {
					GpioPin.Write((GpioPinValue) state);
					Logger?.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state with ({mode.ToString()}) mode.");
					Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, state, mode));
					return true;
				}

				Logger?.Trace($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.");
				Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, mode));
				return true;
			}
			finally {
				Cast<IGpioControllerDriver>(this).UpdatePinConfig(GetPinConfig(pin));
			}
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				Logger?.Log("The specified pin is invalid.");
				return GpioPinState.Off;
			}

			try {
				GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
				return (GpioPinState) gpioPin.ReadValue();
			}
			finally {
				Cast<IGpioControllerDriver>(this).UpdatePinConfig(GetPinConfig(pin));
			}
		}

		public bool GpioDigitalRead(int pin) {
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				Logger?.Log("The specified pin is invalid.");
				return false;
			}

			try {
				GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
				return gpioPin.Read();
			}
			finally {
				Cast<IGpioControllerDriver>(this).UpdatePinConfig(GetPinConfig(pin));
			}
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!PinController.IsValidPin(bcmPin)) {
				Logger?.Log("The specified pin is invalid.");
				return 0;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[bcmPin];
			return GpioPin.PhysicalPinNumber;
		}

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver => driver;
	}
}
