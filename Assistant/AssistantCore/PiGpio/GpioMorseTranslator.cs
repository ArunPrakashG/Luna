using Assistant.Extensions;
using Assistant.Log;
using Assistant.MorseCode;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.AssistantCore.PiGpio {
	public class GpioMorseTranslator {
		private MorseCore MorseCore => Core.MorseCode;
		private GpioPinController Controller => Core.PiController.PinController;
		private readonly Logger Logger = new Logger("GPIO-MORSE");
		public bool IsTranslatorOnline { get; private set; }

		public GpioMorseTranslator InitMorseTranslator() {
			if (Controller == null) {
				IsTranslatorOnline = false;
				throw new InvalidOperationException("Cannot start morse translator as the PinController is null!");
			}

			IsTranslatorOnline = true;
			return this;
		}

		public async Task<bool> RelayMorseCycle(string textToConvert, int relayPin) {
			if (Helpers.IsNullOrEmpty(textToConvert)) {
				Logger.Log("The specified text is either null or empty.", Enums.LogLevels.Warn);
				return false;
			}

			if (!PiController.IsValidPin(relayPin) || !Core.Config.RelayPins.Contains(relayPin)) {
				Logger.Log("Please specify a valid relay pin to run the cycle.", Enums.LogLevels.Warn);
				return false;
			}

			Logger.Log($"Converting to morse...", Enums.LogLevels.Info);
			string Morse = MorseCore.ConvertToMorseCode(textToConvert);

			if (Helpers.IsNullOrEmpty(Morse)) {
				Logger.Log("Conversion to morse failed. cannot proceed.", Enums.LogLevels.Warn);
				return false;
			}

			Logger.Log($"TEXT >> {textToConvert}");
			Logger.Log($"MORSE >> {Morse}");

			if (Core.Config.RelayPins.Count() > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					if (pin.Equals(relayPin)) {
						GpioPinConfig pinStatus = Controller.GetGpioConfig(pin);

						if (pinStatus.IsPinOn) {
							Controller.SetGpioValue(pin, Enums.GpioPinMode.Output, Enums.GpioPinState.Off);
						}

						break;
					}
				}
			}

			if (!MorseCore.IsValidMorse(Morse)) {
				Logger.Log("The specified morse is invalid!", Enums.LogLevels.Warn);
				return false;
			}

			string pauseBetweenLetters = "_";     // One Time Unit
			string pauseBetweenWords = "_______"; // Seven Time Unit

			Morse = Morse.Replace("  ", pauseBetweenWords);
			Morse = Morse.Replace(" ", pauseBetweenLetters);

			foreach (char character in Morse.ToCharArray()) {
				switch (character) {
					case '.':
						Controller.SetGpioWithTimeout(relayPin, Enums.GpioPinMode.Output, Enums.GpioPinState.On, TimeSpan.FromMilliseconds(300));
						break;
					case '-':
						Controller.SetGpioWithTimeout(relayPin, Enums.GpioPinMode.Output, Enums.GpioPinState.On, TimeSpan.FromMilliseconds(300 * 3));
						break;
					case '_':
						await Task.Delay(300).ConfigureAwait(false);
						break;
				}
			}

			if (Core.Config.RelayPins.Count() > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinConfig pinStatus = Controller.GetGpioConfig(pin);

					if (pinStatus.IsPinOn) {
						Controller.SetGpioValue(pin, Enums.GpioPinMode.Output, Enums.GpioPinState.Off);
					}

					break;
				}
			}

			return true;
		}
	}
}
