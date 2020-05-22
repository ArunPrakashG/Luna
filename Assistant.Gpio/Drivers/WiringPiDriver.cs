using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Exceptions;
using Assistant.Logging.Interfaces;
using System;
using System.Linq;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {
	public class WiringPiDriver : IGpioControllerDriver {
		private const string COMMAND_KEY = "gpio -g";

		public ILogger Logger { get; private set; }

		public AvailablePins AvailablePins { get; private set; }

		public bool IsDriverInitialized { get; private set; }

		public NumberingScheme NumberingScheme { get; private set; }

		public PinConfig PinConfig => PinConfigManager.GetConfiguration();

		public GpioDriver DriverName => GpioDriver.WiringPiDriver;

		private static bool IsLibraryInstalled;

		private bool IsWiringPiInstalled() {
			if (IsLibraryInstalled) {
				return true;
			}

			string? executeResult = "gpio".ExecuteBash(false);

			if (string.IsNullOrEmpty(executeResult)) {
				return false;
			}

			// 10/10 hecks
			if (executeResult.Contains("your", StringComparison.OrdinalIgnoreCase) && executeResult.Contains("service", StringComparison.OrdinalIgnoreCase)) {
				IsLibraryInstalled = true;
				return true;
			}

			return false;
		}

		public IGpioControllerDriver InitDriver(ILogger _logger, AvailablePins _availablePins, NumberingScheme _scheme) {
			Logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			AvailablePins = _availablePins;

			if (!IsWiringPiInstalled()) {
				throw new DriverInitializationFailedException(nameof(WiringPiDriver), "WiringPi Library isn't installed on the system.");
			}

			if (!GpioCore.IsAllowedToExecute) {
				IsDriverInitialized = false;
				throw new DriverInitializationFailedException(nameof(WiringPiDriver), "Driver isn't allowed to execute.");
			}

			NumberingScheme = _scheme;

			//for (int i = 0; i < AvailablePins.OutputPins.Length; i++) {
			//	SetMode(AvailablePins.OutputPins[i], GpioPinMode.Output);
			//}

			//for (int i = 0; i < AvailablePins.InputPins.Length; i++) {
			//	SetMode(AvailablePins.InputPins[i], GpioPinMode.Input);
			//}

			IsDriverInitialized = true;
			return this;
		}

		public IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver => driver;

		public Pin GetPinConfig(int pinNumber) {
			if (!IsWiringPiInstalled()) {
				return new Pin();
			}

			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return new Pin();
			}

			if (!PinController.IsValidPin(pinNumber)) {
				return new Pin();
			}

			return new Pin(pinNumber, ReadState(pinNumber), GetMode(pinNumber));
		}

		public bool GpioDigitalRead(int pin) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return false;
			}

			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			return ReadState(pin) == Enums.GpioPinState.Off ? false : true;
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			Logger.Warning(nameof(GpioPhysicalPinNumber) + " method is not supported when using WiringPiDriver.");
			return -1;
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!IsWiringPiInstalled()) {
				return GpioPinState.Off;
			}

			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return GpioPinState.Off;
			}

			if (!PinController.IsValidPin(pin)) {
				return GpioPinState.Off;
			}

			return ReadState(pin);
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return false;
			}

			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			return SetMode(pin, mode);
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return false;
			}

			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			return SetMode(pin, mode) ? WriteValue(pin, state) : false;
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!IsWiringPiInstalled()) {
				return false;
			}

			if (!GpioCore.IsAllowedToExecute || !IsDriverInitialized) {
				return false;
			}

			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			Pin pinConfig = GetPinConfig(pin);

			if (pinConfig.Mode != GpioPinMode.Output) {
				return false;
			}

			return WriteValue(pin, state);
		}

		private GpioPinMode GetMode(int pinNumber) {
			if (pinNumber < 0) {
				return GpioPinMode.Input;
			}

			return AvailablePins.OutputPins.Contains(pinNumber) ? GpioPinMode.Output : GpioPinMode.Input;
		}

		private GpioPinState ReadState(int pinNumber) {
			if (!PinController.IsValidPin(pinNumber)) {
				return GpioPinState.Off;
			}

			string? result = (COMMAND_KEY + " read " + pinNumber).ExecuteBash(false);

			if (string.IsNullOrEmpty(result)) {
				return GpioPinState.Off;
			}

			if (!int.TryParse(result, out int state)) {
				return GpioPinState.Off;
			}

			return (GpioPinState) state;
		}

		private bool SetMode(int pinNumber, GpioPinMode mode) {
			if (!PinController.IsValidPin(pinNumber)) {
				return false;
			}

			string pinMode = mode == GpioPinMode.Input ? "in" : "out";
			(COMMAND_KEY + $" mode {pinNumber} {pinMode}").ExecuteBash(false);
			return true;
		}

		private bool WriteValue(int pinNumber, GpioPinState state) {
			if (!PinController.IsValidPin(pinNumber)) {
				return false;
			}

			(COMMAND_KEY + $" write {pinNumber} {(int) state}").ExecuteBash(false);
			return true;
		}
		
		public bool TogglePinState(int pinNumber) {
			if (!PinController.IsValidPin(pinNumber)) {
				return false;
			}

			(COMMAND_KEY + $" toggle {pinNumber}").ExecuteBash(false);
			return true;
		}
	}
}
