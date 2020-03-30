using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using System.Device.Gpio;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	internal class SystemDeviceDriver : IGpioControllerDriver {
		private System.Device.Gpio.GpioController? DriverController { get; set; }

		public bool IsDriverProperlyInitialized { get; private set; }
		public Enums.EGPIO_DRIVERS DriverName => Enums.EGPIO_DRIVERS.SystemDevicesDriver;

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		public NumberingScheme NumberingScheme { get; set; }

		public IGpioControllerDriver InitDriver(NumberingScheme numberingScheme) {
			if (!Controllers.GpioController.IsAllowedToExecute) {
				Cast((IGpioControllerDriver) this).Logger.Warning("Failed to initialize Gpio Controller Driver.");
				IsDriverProperlyInitialized = false;
				return null;
			}

			NumberingScheme = numberingScheme;
			DriverController = new System.Device.Gpio.GpioController((PinNumberingScheme) numberingScheme);
			IsDriverProperlyInitialized = true;
			return this;
		}

		public Pin? GetPinConfig(int pinNumber) {
			if (!IOController.IsValidPin(pinNumber) || DriverController == null || !IsDriverProperlyInitialized) {
				return null;
			}

			PinValue value = DriverController.Read(pinNumber);
			PinMode mode = DriverController.GetPinMode(pinNumber);
			Pin config = new Pin(pinNumber, value == PinValue.High ? GpioPinState.Off : GpioPinState.On, mode == PinMode.Input ? GpioPinMode.Input : GpioPinMode.Output);
			return config;
		}

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver {
			return driver;
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!IOController.IsValidPin(pin) || !IsDriverProperlyInitialized) {
				return false;
			}

			try {
				if (DriverController == null) {
					return false;
				}

				if (!DriverController.IsPinModeSupported(pin, (PinMode) mode)) {
					return false;
				}

				DriverController.OpenPin(pin);

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				DriverController.SetPinMode(pin, (PinMode) mode);
				Cast<IGpioControllerDriver>(this).Logger.Trace($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.");
				Cast<IGpioControllerDriver>(this).UpdatePinConfig(new Pin(pin, mode));
				return true;
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(pin)) {
						DriverController.ClosePin(pin);
					}
				}

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
				if (DriverController == null) {
					return false;
				}

				if (!DriverController.IsPinModeSupported(pin, (PinMode) mode)) {
					return false;
				}

				DriverController.OpenPin(pin);

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				DriverController.SetPinMode(pin, (PinMode) mode);
				DriverController.Write(pin, state == GpioPinState.Off ? PinValue.High : PinValue.Low);
				Cast<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state with ({mode.ToString()}) mode.");
				Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, state, mode));
				return true;
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(pin)) {
						DriverController.ClosePin(pin);
					}
				}

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
				if (DriverController == null) {
					return GpioPinState.Off;
				}

				DriverController.OpenPin(pin);

				if (!DriverController.IsPinOpen(pin)) {
					return GpioPinState.Off;
				}

				return DriverController.Read(pin) == PinValue.High ? GpioPinState.Off : GpioPinState.On;
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(pin)) {
						DriverController.ClosePin(pin);
					}
				}

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
				if (DriverController == null) {
					return false;
				}

				DriverController.OpenPin(pin);

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				return !(DriverController.Read(pin) == PinValue.High);
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(pin)) {
						DriverController.ClosePin(pin);
					}
				}

				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!IOController.IsValidPin(pin) || !IsDriverProperlyInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			try {
				if (DriverController == null) {
					return false;
				}

				DriverController.OpenPin(pin);

				if (!DriverController.IsPinOpen(pin)) {
					return false;
				}

				DriverController.Write(pin, state == GpioPinState.Off ? PinValue.High : PinValue.Low);
				Cast<IGpioControllerDriver>(this)?.Logger.Trace($"Configured ({pin}) gpio pin to ({state.ToString()}) state.");
				Cast<IGpioControllerDriver>(this)?.UpdatePinConfig(new Pin(pin, state));
				return true;
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(pin)) {
						DriverController.ClosePin(pin);
					}
				}

				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!IOController.IsValidPin(bcmPin) || !IsDriverProperlyInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return -1;
			}

			try {
				if (DriverController == null) {
					return -1;
				}

				DriverController.OpenPin(bcmPin);

				if (!DriverController.IsPinOpen(bcmPin)) {
					return -1;
				}

				Cast<IGpioControllerDriver>(this)?.Logger.Info("System.Devices.Gpio driver doesn't support PhysicalPinNumber conversion.");
				return -1;
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(bcmPin)) {
						DriverController.ClosePin(bcmPin);
					}
				}
			}
		}
	}
}
