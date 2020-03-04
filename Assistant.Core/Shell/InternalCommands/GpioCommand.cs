using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Drivers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Gpio.Enums;

namespace Assistant.Core.Shell.InternalCommands {
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
				ShellOut.Info($"{CommandName} - {CommandKey} | {CommandDescription} | gpio -[pin_value], -[pin_mode], -[pin_state], -[delay_mins];");
				return;
			}

			ShellOut.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellOut.Info($"|> {CommandDescription}");
			ShellOut.Info($"Basic Syntax -> ' gpio -[pin_value]; '");
			ShellOut.Info($"Basic with Pin Mode -> ' gpio -[pin_value], -[pin_mode]; '");
			ShellOut.Info($"Advanced -> ' gpio -[pin_value], -[pin_mode], -[pin_state]; '");
			ShellOut.Info($"Advanced with delay -> ' gpio -[pin_value], -[pin_mode], -[pin_state], -[delay_mins]; '");
			ShellOut.Info($"----------------- ----------------------------- -----------------");
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (!Core.CoreInitiationCompleted) {
				ShellOut.Error("Cannot execute as the core hasn't been successfully started yet.");
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellOut.Error("Too many arguments.");
				return;
			}

			if (!PiGpioController.IsAllowedToExecute) {
				ShellOut.Error("Gpio functions are not allowed to execute.");
				ShellOut.Info("Gpio pin controlling functions are only available on raspberry pi with an OS such as Raspbian.");
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
					ShellOut.Error("Gpio pin, Pin state, pin mode values are not specified.");
					return;
				}

				if (string.IsNullOrEmpty(parameter.Parameters[0])) {
					ShellOut.Error("Gpio pin is invalid or not specified.");
					return;
				}

				int pin;
				GpioPinMode pinMode;
				GpioPinState pinState;
				bool isSet;

				IGpioControllerDriver? driver = PinController.GetDriver();
				if (driver == null || !driver.IsDriverProperlyInitialized) {
					ShellOut.Error("Internal error occurred with the gpio driver. Please restart the assistant.");
					return;
				}

				switch (parameter.ParameterCount) {
					case 1 when !string.IsNullOrEmpty(parameter.Parameters[0]):
						ShellOut.Info("Note: as only 1 argument is specified, the default value will be set for the specified pin.");

						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellOut.Error("Failed to parse gpio pin value.");
							return;
						}

						ShellOut.Info($"{pin} will be set to Output mode and configured in On state.");

						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Core.Config.OutputModePins.Contains(pin)) {
							ShellOut.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);

						if (!isSet) {
							ShellOut.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellOut.Info($"Successfully configured {pin} gpio pin.");
						return;
					case 2 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]):
						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellOut.Error("Failed to parse gpio pin value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[1], out int modeVal)) {
							ShellOut.Error("Failed to parse gpio pin mode value.");
							return;
						}

						pinMode = (GpioPinMode) modeVal;

						ShellOut.Info($"{pin} will be set to {pinMode.ToString()} mode and configured in On state.");

						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Core.Config.OutputModePins.Contains(pin)) {
							ShellOut.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioValue(pin, pinMode, GpioPinState.On);

						if (!isSet) {
							ShellOut.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellOut.Info($"Successfully configured {pin} gpio pin.");
						return;
					case 3 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]) &&
					!string.IsNullOrEmpty(parameter.Parameters[2]):
						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellOut.Error("Failed to parse gpio pin value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[1], out int pinModeVal)) {
							ShellOut.Error("Failed to parse gpio pin mode value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[2], out int stateVal)) {
							ShellOut.Error("Failed to parse gpio pin state value.");
							return;
						}

						pinMode = (GpioPinMode) pinModeVal;
						pinState = (GpioPinState) stateVal;
						ShellOut.Info($"{pin} will be set to {pinMode.ToString()} mode and configured in {pinState} state.");

						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Core.Config.OutputModePins.Contains(pin)) {
							ShellOut.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioValue(pin, pinMode, pinState);

						if (!isSet) {
							ShellOut.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellOut.Info($"Successfully configured {pin} gpio pin.");
						return;
					case 4 when !string.IsNullOrEmpty(parameter.Parameters[0]) &&
					!string.IsNullOrEmpty(parameter.Parameters[1]) &&
					!string.IsNullOrEmpty(parameter.Parameters[2]) &&
					!string.IsNullOrEmpty(parameter.Parameters[3]):
						if (!int.TryParse(parameter.Parameters[0], out pin)) {
							ShellOut.Error("Failed to parse gpio pin value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[1], out int modeValue)) {
							ShellOut.Error("Failed to parse gpio pin mode value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[2], out int stateValue)) {
							ShellOut.Error("Failed to parse gpio pin state value.");
							return;
						}

						if (!int.TryParse(parameter.Parameters[2], out int delayValue)) {
							ShellOut.Error("Failed to parse gpio pin state value.");
							return;
						}

						pinMode = (GpioPinMode) modeValue;
						pinState = (GpioPinState) stateValue;
						ShellOut.Info($"{pin} will be set to {pinMode.ToString()} mode and configured in {pinState} state and set back by a delay of {delayValue} minutes.");
						if (!Constants.BcmGpioPins.Contains(pin) || !PinController.IsValidPin(pin) || !Core.Config.OutputModePins.Contains(pin)) {
							ShellOut.Error("Specified gpio pin is an invalid.");
							return;
						}

						isSet = driver.SetGpioWithTimeout(pin, pinMode, pinState, TimeSpan.FromMinutes(delayValue));

						if (!isSet) {
							ShellOut.Error($"Failed to configure {pin} gpio pin. Please validate the pin argument.");
							return;
						}

						ShellOut.Info($"Successfully configured {pin} gpio pin.");
						return;
					default:
						ShellOut.Error("Command seems to be in incorrect syntax.");
						return;
				}
			}
			catch (Exception e) {
				ShellOut.Exception(e);
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
