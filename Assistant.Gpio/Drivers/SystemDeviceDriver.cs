using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using System.Device.Gpio;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	public class SystemDeviceDriver : IGpioControllerDriver {
		private System.Device.Gpio.GpioController? DriverController { get; set; }

		public bool IsDriverInitialized { get; private set; }
		public Enums.GpioDriver DriverName => Enums.GpioDriver.SystemDevicesDriver;

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		public NumberingScheme NumberingScheme { get; set; }

		public IGpioControllerDriver InitDriver(NumberingScheme numberingScheme) {
			if (!Controllers.GpioController.IsAllowedToExecute) {
				Cast((IGpioControllerDriver) this).Logger.Warning("Failed to initialize Gpio Controller Driver.");
				IsDriverInitialized = false;
				return null;
			}

			NumberingScheme = numberingScheme;
			DriverController = new System.Device.Gpio.GpioController((PinNumberingScheme) numberingScheme);
			IsDriverInitialized = true;
			return this;
		}

		public Pin? GetPinConfig(int pinNumber) {
			if (!PinController.IsValidPin(pinNumber) || DriverController == null || !IsDriverInitialized) {
				return null;
			}

			if (DriverController == null) {
				return null;
			}

			try {
				if (!DriverController.IsPinOpen(pinNumber)) {
					DriverController.OpenPin(pinNumber);
				}

				if (!DriverController.IsPinOpen(pinNumber)) {
					return null;
				}

				PinValue value = DriverController.Read(pinNumber);
				PinMode mode = DriverController.GetPinMode(pinNumber);
				Pin config = new Pin(pinNumber, value == PinValue.High ? GpioPinState.Off : GpioPinState.On, mode == PinMode.Input ? GpioPinMode.Input : GpioPinMode.Output);
				return config;
			}
			finally {
				if (DriverController != null) {
					if (DriverController.IsPinOpen(pinNumber)) {
						DriverController.ClosePin(pinNumber);
					}
				}
			}
		}

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver {
			return driver;
		}

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
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
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
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
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
			if (!PinController.IsValidPin(pin) || !IsDriverInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
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
			if (!PinController.IsValidPin(bcmPin) || !IsDriverInitialized) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
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
