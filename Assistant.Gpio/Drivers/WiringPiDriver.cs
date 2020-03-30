using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.Gpio.Drivers {
	internal class WiringPiDriver : IGpioControllerDriver {
		private const string COMMAND_KEY = "gpio -g";
		public bool IsDriverProperlyInitialized { get; private set; }

		public Enums.NumberingScheme NumberingScheme { get; set; }

		public Enums.EGPIO_DRIVERS DriverName => Enums.EGPIO_DRIVERS.WiringPiDriver;

		private bool IsWiringPiInstalled() {
			string? executeResult = "gpio".ExecuteBash(false);

			if (string.IsNullOrEmpty(executeResult)) {
				return false;
			}

			// 10/10 hecks
			if (executeResult.Contains("your", StringComparison.OrdinalIgnoreCase) && executeResult.Contains("service", StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			return false;
		}

		public IGpioControllerDriver InitDriver(Enums.NumberingScheme scheme) {
			if (!IsWiringPiInstalled()) {
				Cast<IGpioControllerDriver>(this)?.Logger.Warning("Failed to initialize Gpio Controller Driver. (Driver isn't allowed to execute.)");
				return this;
			}

			if (!GpioController.IsAllowedToExecute) {
				Cast<IGpioControllerDriver>(this)?.Logger.Warning("Failed to initialize Gpio Controller Driver. (Driver isn't allowed to execute.)");
				IsDriverProperlyInitialized = false;
				return this;
			}

			NumberingScheme = scheme;
			for (int i = 0; i < GpioController.AvailablePins.OutputPins.Length; i++) {
				SetMode(GpioController.AvailablePins.OutputPins[i], Enums.GpioPinMode.Output);
				Task.Delay(10).Wait();
			}

			for (int i = 0; i < GpioController.AvailablePins.InputPins.Length; i++) {
				SetMode(GpioController.AvailablePins.InputPins[i], Enums.GpioPinMode.Input);
				Task.Delay(10).Wait();
			}

			IsDriverProperlyInitialized = true;
			return this;
		}

		public PinConfig PinConfig => throw new NotImplementedException();

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver {
			return driver;
		}

		public Pin? GetPinConfig(int pinNumber) {
			if (!IsWiringPiInstalled()) {
				return null;
			}

			if (!GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return null;
			}

			if (!IOController.IsValidPin(pinNumber)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return null;
			}

			return new Pin(pinNumber, ReadState(pinNumber), GetMode(pinNumber));
		}

		public bool GpioDigitalRead(int pin) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return false;
			}

			if (!IOController.IsValidPin(pin)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			try {
				return ReadState(pin) == Enums.GpioPinState.Off ? false : true;
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			Cast<IGpioControllerDriver>(this)?.Logger.Warning(nameof(GpioPhysicalPinNumber) + " method is not supported when using WiringPiDriver.");
			return -1;
		}

		public Enums.GpioPinState GpioPinStateRead(int pin) {
			if (!IsWiringPiInstalled()) {
				return Enums.GpioPinState.Off;
			}

			if (!GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return Enums.GpioPinState.Off;
			}

			if (!IOController.IsValidPin(pin)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return Enums.GpioPinState.Off;
			}

			try {
				return ReadState(pin);
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return false;
			}

			if (!IOController.IsValidPin(pin)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			try {
				return SetMode(pin, mode);
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}			
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode, Enums.GpioPinState state) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return false;
			}

			if (!IOController.IsValidPin(pin)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			try {
				if (SetMode(pin, mode)) {
					return WriteValue(pin, state);
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

		public bool SetGpioValue(int pin, Enums.GpioPinState state) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioController.IsAllowedToExecute || !IsDriverProperlyInitialized) {
				return false;
			}

			if (!IOController.IsValidPin(pin)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			try {
				Pin? pinConfig = GetPinConfig(pin);

				if (pinConfig == null) {
					return false;
				}

				if (pinConfig.Mode != Enums.GpioPinMode.Output) {
					Cast<IGpioControllerDriver>(this)?.Logger.Warning($"Pin cannot be configured to {state.ToString()} as it is not in output mode.");
					return false;
				}

				return WriteValue(pin, state);
			}
			finally {
				Pin? pinConfig = GetPinConfig(pin);
				if (pinConfig != null) {
					Cast<IGpioControllerDriver>(this).UpdatePinConfig(pinConfig);
				}
			}
		}

		private Enums.GpioPinMode GetMode(int pinNumber) {
			if (pinNumber < 0) {
				return Enums.GpioPinMode.Input;
			}

			if (GpioController.AvailablePins.OutputPins.Contains(pinNumber)) {
				return Enums.GpioPinMode.Output;
			}

			return Enums.GpioPinMode.Input;
		}

		private Enums.GpioPinState ReadState(int pinNumber) {
			if (!IOController.IsValidPin(pinNumber)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return Enums.GpioPinState.Off;
			}

			string? result = (COMMAND_KEY + " read " + pinNumber).ExecuteBash(false);

			if (string.IsNullOrEmpty(result)) {
				return Enums.GpioPinState.Off;
			}

			if (!int.TryParse(result, out int state)) {
				return Enums.GpioPinState.Off;
			}

			return (Enums.GpioPinState) state;
		}

		private bool SetMode(int pinNumber, Enums.GpioPinMode mode) {
			if (!IOController.IsValidPin(pinNumber)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			string pinMode = mode == Enums.GpioPinMode.Input ? "in" : "out";
			(COMMAND_KEY + $" mode {pinNumber} {pinMode}").ExecuteBash(false);
			return true;
		}

		private bool WriteValue(int pinNumber, Enums.GpioPinState state) {
			if (!IOController.IsValidPin(pinNumber)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			(COMMAND_KEY + $" write {pinNumber} {(int) state}").ExecuteBash(false);
			return true;
		}

		/// <summary>
		/// Wiring Pi has a command to toggle the pin state directly.
		/// We do have a global method for toggling implemented so won't be using this method unless something went wrong.
		/// </summary>
		/// <param name="pinNumber"></param>
		/// <returns></returns>
		[Obsolete]
		private bool TogglePin(int pinNumber) {
			if (!IOController.IsValidPin(pinNumber)) {
				Cast<IGpioControllerDriver>(this)?.Logger.Log("The specified pin is invalid.");
				return false;
			}

			(COMMAND_KEY + $" toggle {pinNumber}").ExecuteBash(false);
			return true;
		}
	}
}
