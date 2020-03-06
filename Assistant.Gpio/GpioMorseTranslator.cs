using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Drivers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Morse;
using System;
using System.Threading.Tasks;
using static Assistant.Gpio.Config.PinConfig;
using static Assistant.Gpio.Enums;
using static Assistant.Logging.Enums;

namespace Assistant.Gpio {
	public class GpioMorseTranslator {
		private readonly ILogger Logger = new Logger(typeof(GpioMorseTranslator).Name);
		private readonly MorseCore MorseCore = new MorseCore();
		private IGpioControllerDriver? Driver => PinController.GetDriver();
		public readonly bool IsTranslatorOnline;

		public GpioMorseTranslator() {
			if (Driver == null) {
				IsTranslatorOnline = false;
				throw new InvalidOperationException("Cannot start Morse translator as the PinController is null!");
			}

			IsTranslatorOnline = true;
		}

		public async Task<bool> RelayMorseCycle(string textToConvert, int relayPin) {
			if (string.IsNullOrEmpty(textToConvert)) {
				Logger.Log("The specified text is either null or empty.", LogLevels.Warn);
				return false;
			}

			if (Driver == null) {
				Logger.Log("Malfunctioning PinController.", LogLevels.Warn);
				return false;
			}

			if (!PinController.IsValidPin(relayPin)) {
				Logger.Log("Please specify a valid relay pin to run the cycle.", LogLevels.Warn);
				return false;
			}

			Logger.Log($"Converting to Morse...", LogLevels.Info);
			string Morse = MorseCore.ConvertToMorseCode(textToConvert);

			if (string.IsNullOrEmpty(Morse)) {
				Logger.Log("Conversion to Morse failed. cannot proceed.", LogLevels.Warn);
				return false;
			}

			Logger.Log($"TEXT >> {textToConvert}");
			Logger.Log($"MORSE >> {Morse}");

			Pin beforePinStatus = Driver.GetPinConfig(relayPin);

			if (beforePinStatus.IsPinOn) {
				Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			if (!MorseCore.IsValidMorse(Morse)) {
				Logger.Log("The specified Morse is invalid!", LogLevels.Warn);
				return false;
			}

			string pauseBetweenLetters = "_";     // One Time Unit
			string pauseBetweenWords = "_______"; // Seven Time Unit

			Morse = Morse.Replace("  ", pauseBetweenWords);
			Morse = Morse.Replace(" ", pauseBetweenLetters);

			foreach (char character in Morse.ToCharArray()) {
				switch (character) {
					case '.':
						Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300));
						break;
					case '-':
						Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300 * 3));
						break;
					case '_':
						await Task.Delay(300).ConfigureAwait(false);
						break;
				}
			}

			Pin afterPinStatus = Driver.GetPinConfig(relayPin);

			if (afterPinStatus.IsPinOn) {
				Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			return true;
		}
	}
}
