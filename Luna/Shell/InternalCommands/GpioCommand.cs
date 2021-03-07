using Luna.Gpio;
using Luna.Gpio.Controllers;
using Luna.Gpio.Drivers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Shell.InternalCommands {
	public class GpioCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Gpio Command";

		public bool IsInitSuccess { get; set; }

		public int MaxParameterCount => 4;

		public string CommandDescription => "Command to control gpio pins.";

		public string CommandKey => "gpio";

		public SemaphoreSlim Sync { get; set; }
		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public void Dispose() {
			IsInitSuccess = false;
			Sync.Dispose();
		}

		public void OnHelpExec(bool quickHelp) {
			if (quickHelp) {
				ShellIO.Info($"{CommandName} - {CommandKey} | {CommandDescription} | {CommandKey} -[pin_value] -[pin_mode] -[pin_state] -[delay_mins]");
				return;
			}

			ShellIO.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellIO.Info($"|> {CommandDescription}");
			ShellIO.Info($"Basic Syntax -> ' {CommandKey} -[pin_value] '");
			ShellIO.Info($"Basic with Pin Mode -> ' {CommandKey} -[pin_value] -[pin_mode] '");
			ShellIO.Info($"Advanced -> ' {CommandKey} -[pin_value] -[pin_mode] -[pin_state] '");
			ShellIO.Info($"Advanced with delay -> ' {CommandKey} -[pin_value] -[pin_mode] -[pin_state] -[delay_mins] '");
			ShellIO.Info($"----------------- ----------------------------- -----------------");
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (!Program.CoreInstance.IsBaseInitiationCompleted) {
				ShellIO.Error("Cannot execute as the core hasn't been successfully started yet.");
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellIO.Error("Too many arguments.");
				return;
			}

			if (!GpioCore.IsAllowedToExecute) {
				ShellIO.Error("Gpio functions are not allowed to execute.");
				ShellIO.Info("Gpio pin controlling functions are only available on raspberry pi with an OS such as Raspbian.");
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (OnExecuteFunc != null) {
					if (OnExecuteFunc.Invoke(parameter)) {
						return;
					}
				}

				if (parameter.Parameters == null || parameter.Parameters.Length == 0) {
					ShellIO.Error("Gpio pin, Pin state, pin mode values are not specified.");
					return;
				}

				if (string.IsNullOrEmpty(parameter.Parameters[0])) {
					ShellIO.Error("Gpio pin is invalid or not specified.");
					return;
				}

				int pin;
				GpioPinMode pinMode;
				GpioPinState pinState;
				bool isSet;

				GpioControllerDriver? driver = PinController.GetDriver();
				if (driver == null || !driver.IsDriverInitialized) {
					ShellIO.Error("Internal error occurred with the gpio driver. Please restart the assistant.");
					return;
				}

				switch (parameter.ParameterCount) {
					case 1 when !string.IsNullOrEmpty(parameter.Parameters[0]):
						ShellIO.Info("Note: as only 1 argument is specified, the default value will be set for the specified pin.");

						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellIO.Error("Failed to parse gpio pin value.");
							return;
						}

						ShellIO.Info($"{pin} will be set to Output mode and configured in On state.");

						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Program.CoreInstance.GetCoreConfig().GpioConfiguration.OutputModePins.Contains(pin)) {
							ShellIO.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.TogglePinState(pin);

						if (!isSet) {
							ShellIO.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellIO.Info($"Successfully configured {pin} gpio pin.");
						return;
					case 2 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]) &&
					parameter.Parameters[0].Equals("relay", StringComparison.OrdinalIgnoreCase):
						if (!int.TryParse(parameter.Parameters[1], out int relayNum)) {
							ShellIO.Error("Failed to parse relay number value.");
							return;
						}

						if (!PinController.IsValidPin(Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNum])) {
							ShellIO.Error($"The pin ' {Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNum]} ' is invalid.");
							return;
						}

						isSet = driver.TogglePinState(Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNum]);

						if (!isSet) {
							ShellIO.Error($"Failed to configure {Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNum]} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellIO.Info($"Successfully configured {Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNum]} gpio pin.");
						return;
					case 2 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]):
						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellIO.Error("Failed to parse gpio pin value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[1], out int modeVal)) {
							ShellIO.Error("Failed to parse gpio pin mode value.");
							return;
						}

						pinMode = (GpioPinMode) modeVal;

						ShellIO.Info($"{pin} will be set to {pinMode.ToString()} mode and configured in On state.");

						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Program.CoreInstance.GetCoreConfig().GpioConfiguration.OutputModePins.Contains(pin)) {
							ShellIO.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioValue(pin, pinMode, GpioPinState.On);

						if (!isSet) {
							ShellIO.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellIO.Info($"Successfully configured {pin} gpio pin.");
						return;
					case 3 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]) &&
					!string.IsNullOrEmpty(parameter.Parameters[2]):
						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellIO.Error("Failed to parse gpio pin value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[1], out int pinModeVal)) {
							ShellIO.Error("Failed to parse gpio pin mode value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[2], out int stateVal)) {
							ShellIO.Error("Failed to parse gpio pin state value.");
							return;
						}

						pinMode = (GpioPinMode) pinModeVal;
						pinState = (GpioPinState) stateVal;
						ShellIO.Info($"{pin} will be set to {pinMode.ToString()} mode and configured in {pinState} state.");

						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Program.CoreInstance.GetCoreConfig().GpioConfiguration.OutputModePins.Contains(pin)) {
							ShellIO.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioValue(pin, pinMode, pinState);

						if (!isSet) {
							ShellIO.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellIO.Info($"Successfully configured {pin} gpio pin.");
						return;
					case 4 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]) &&
					!string.IsNullOrEmpty(parameter.Parameters[2]) &&
					!string.IsNullOrEmpty(parameter.Parameters[3]):
						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellIO.Error("Failed to parse gpio pin value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[1], out int modeValue)) {
							ShellIO.Error("Failed to parse gpio pin mode value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[2], out int stateValue)) {
							ShellIO.Error("Failed to parse gpio pin state value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[3], out int delayValue)) {
							ShellIO.Error("Failed to parse gpio pin state value.");
							return;
						}

						pinMode = (GpioPinMode) modeValue;
						pinState = (GpioPinState) stateValue;
						ShellIO.Info($"{pin} will be set to {pinMode.ToString()} mode and configured in {pinState} state and set back by a delay of {delayValue} minutes.");
						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Program.CoreInstance.GetCoreConfig().GpioConfiguration.OutputModePins.Contains(pin)) {
							ShellIO.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioValue(pin, pinMode, pinState, TimeSpan.FromMinutes(delayValue), false);

						if (!isSet) {
							ShellIO.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellIO.Info($"Successfully configured {pin} gpio pin.");
						return;
					default:
						ShellIO.Error("Command seems to be in incorrect syntax.");
						return;
				}
			}
			catch (Exception e) {
				ShellIO.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		public async Task InitAsync() {
			Sync = new SemaphoreSlim(1, 1);
			IsInitSuccess = true;
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
