using Luna.CommandLine;
using Luna.Gpio.Controllers;
using Luna.Gpio.Exceptions;
using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.Drivers {
	internal class WiringPiDriver : GpioControllerDriver {
		private const string COMMAND_KEY = "gpio -g";
		private static bool IsLibraryInstalled;
		private readonly OSCommandLineInterfacer CommandLine;

		internal WiringPiDriver(InternalLogger logger, PinsWrapper pins, PinConfig pinConfig, NumberingScheme scheme) : base(logger, pins, GpioDriver.WiringPiDriver, pinConfig, scheme) {
			CommandLine = new OSCommandLineInterfacer(OSPlatform.Linux, false, false, false);
		}

		private bool IsWiringPiInstalled() {
			if (IsLibraryInstalled) {
				return true;
			}

			string? executeResult = CommandLine.Execute("gpio");

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

		internal override GpioControllerDriver Init() {
			if (!IsWiringPiInstalled()) {
				throw new DriverInitializationFailedException(nameof(WiringPiDriver), "WiringPi Library isn't installed on the system.");
			}

			if (!GpioCore.IsAllowedToExecute) {
				throw new DriverInitializationFailedException(nameof(WiringPiDriver), "Driver isn't allowed to execute.");
			}

			return this;
		}

		internal override Pin GetPinConfig(int pinNumber) {
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

		internal override bool GpioDigitalRead(int pin) {
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

		internal override int GpioPhysicalPinNumber(int bcmPin) {
			Logger.Warn(nameof(GpioPhysicalPinNumber) + " method is not supported when using WiringPiDriver.");
			return -1;
		}

		internal override GpioPinState GpioPinStateRead(int pin) {
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

		internal override bool SetGpioValue(int pin, Enums.GpioPinMode mode) {
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

		internal override bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
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

		internal override bool SetGpioValue(int pin, GpioPinState state) {
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

			return Pins.OutputPins.Contains(pinNumber) ? GpioPinMode.Output : GpioPinMode.Input;
		}

		private GpioPinState ReadState(int pinNumber) {
			if (!PinController.IsValidPin(pinNumber)) {
				return GpioPinState.Off;
			}

			string? result = (COMMAND_KEY + " read " + pinNumber).ExecuteBash();

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
			(COMMAND_KEY + $" mode {pinNumber} {pinMode}").ExecuteBash();
			return true;
		}

		private bool WriteValue(int pinNumber, GpioPinState state) {
			if (!PinController.IsValidPin(pinNumber)) {
				return false;
			}

			(COMMAND_KEY + $" write {pinNumber} {(int) state}").ExecuteBash();
			return true;
		}

		internal override bool TogglePinState(int pinNumber) {
			if (!PinController.IsValidPin(pinNumber)) {
				return false;
			}

			(COMMAND_KEY + $" toggle {pinNumber}").ExecuteBash();
			return true;
		}
	}
}
