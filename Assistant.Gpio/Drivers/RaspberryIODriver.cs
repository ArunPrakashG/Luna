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
			if (!Controllers.GpioController.IsAllowedToExecute) {
				Cast((IGpioControllerDriver) this)?.Logger.Warning("Failed to initialize Gpio Controller Driver. (Driver isn't allowed to execute.)");
				IsDriverProperlyInitialized = false;
				return this;
			}

			NumberingScheme = scheme;
			Pi.Init<BootstrapWiringPi>();
			IsDriverProperlyInitialized = true;
			return this;
		}

		public Pin? GetPinConfig(int pinNumber) {
			if (!Controllers.GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return null;
			}

			if (!IOController.IsValidPin(pinNumber)) {
				Cast<IGpioControllerDriver>(this).Logger.Log("The specified pin is invalid.");
				return null;
			}

			GpioPin pin = (GpioPin) Pi.Gpio[pinNumber];
			return new Pin(pinNumber, (GpioPinState) pin.ReadValue(), (GpioPinMode) pin.PinMode);
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!IOController.IsValidPin(pin)) {
				return false;
			}

			try {
				GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
				GpioPin.PinMode = (GpioPinDriveMode) mode;
				Cast<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.");
				Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, mode));
				return true;
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}			
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!IOController.IsValidPin(pin)) {
				return false;
			}

			try {
				GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];

				if (GpioPin.PinMode == GpioPinDriveMode.Output) {
					GpioPin.Write((GpioPinValue) state);
					Cast<IGpioControllerDriver>(this).Logger.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state.");
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(new Pin(pin, state));
					return true;
				}

				return false;
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!IOController.IsValidPin(pin) || !IsDriverProperlyInitialized) {
				return false;
			}

			try {
				GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
				GpioPin.PinMode = (GpioPinDriveMode) mode;

				if (mode == GpioPinMode.Output) {
					GpioPin.Write((GpioPinValue) state);
					Cast<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state with ({mode.ToString()}) mode.");
					Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, state, mode));
					return true;
				}

				Cast<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.");
				Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, mode));
				return true;
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!IOController.IsValidPin(pin) || !IsDriverProperlyInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return GpioPinState.Off;
			}

			try {
				GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
				return (GpioPinState) gpioPin.ReadValue();
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}			
		}

		public bool GpioDigitalRead(int pin) {
			if (!IOController.IsValidPin(pin) || !IsDriverProperlyInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			try {
				GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
				return gpioPin.Read();
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!IOController.IsValidPin(bcmPin)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return 0;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[bcmPin];
			return GpioPin.PhysicalPinNumber;
		}

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver {
			return driver;
		}
	}
}
