using Luna.ExternalExtensions.Shared.Shell;
using Luna.Gpio;
using Luna.Gpio.Controllers;
using Luna.Morse;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Shell.InternalCommands {
	public class MorseCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Morse Command";

		public bool IsInitSuccess { get; set; }

		public int MaxParameterCount => 2;

		public string CommandDescription => "Converts specified text to morse code; Generates relay blinking based on morse code.";

		public string CommandKey => "morse";

		public SemaphoreSlim Sync { get; set; }
		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public void Dispose() {
			IsInitSuccess = false;
			Sync.Dispose();
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellOut.Error("Too many arguments.");
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			MorseCore morseCore = MorseRelayTranslator.GetCore();
			string morse = string.Empty;

			try {
				if (OnExecuteFunc != null) {
					if (OnExecuteFunc.Invoke(parameter)) {
						return;
					}
				}

				switch (parameter.ParameterCount) {
					case 1 when !string.IsNullOrEmpty(parameter.Parameters[0]):
						morse = morseCore.ConvertToMorseCode(parameter.Parameters[0]);

						if (string.IsNullOrEmpty(morse) || !morseCore.IsValidMorse(morse)) {
							ShellOut.Error("Failed to verify generated morse code.");
							return;
						}

						ShellOut.Info(">>> " + morse);
						return;
					case 2 when !string.IsNullOrEmpty(parameter.Parameters[0]) && !string.IsNullOrEmpty(parameter.Parameters[1]):
						morse = morseCore.ConvertToMorseCode(parameter.Parameters[0]);

						if (string.IsNullOrEmpty(morse) || !morseCore.IsValidMorse(morse)) {
							ShellOut.Error("Failed to verify generated morse code.");
							return;
						}

						ShellOut.Info(">>> " + morse);
						int relayNumber;

						try {
							if (!int.TryParse(parameter.Parameters[1], out relayNumber)) {
								ShellOut.Error("Relay number argument is invalid.");
								return;
							}
						}
						catch (IndexOutOfRangeException) {
							ShellOut.Error("The specified relay number is invalid is greater than all the available relay numbers.");
							return;
						}

						if (!PinController.IsValidPin(Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNumber])) {
							ShellOut.Error("The specified pin is invalid.");
							return;
						}

						await Program.CoreInstance.GetGpioCore().GetMorseTranslator().RelayMorseCycle(morse, Program.CoreInstance.GetGpioCore().GetAvailablePins().OutputPins[relayNumber]).ConfigureAwait(false);
						ShellOut.Info("Completed!");
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

		public void OnHelpExec(bool quickHelp) {
			if (quickHelp) {
				ShellOut.Info($"{CommandName} - {CommandKey} | {CommandDescription} | {CommandKey} -[text_to_convert]");
				return;
			}

			ShellOut.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellOut.Info($"|> {CommandDescription}");
			ShellOut.Info($"Basic Syntax -> ' {CommandKey} -[text_to_convert] '");
			ShellOut.Info($"Advanced -> ' {CommandKey} -[text_to_convert] -[relay_number_to_morse_cycle] '");
			ShellOut.Info($"----------------- ----------------------------- -----------------");
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
