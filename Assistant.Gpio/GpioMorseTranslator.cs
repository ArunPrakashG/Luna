using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Morse;
using System;
using System.Threading.Tasks;
using static Assistant.Gpio.PiController;
using static Assistant.Logging.Enums;

namespace Assistant.Gpio {
	public class GpioMorseTranslator {
		private MorseCore MorseCore = new MorseCore();
		private Gpio? GpioCore;
		private GpioPinController? Controller => GpioCore?.PinController;
		private readonly ILogger Logger = new Logger("GPIO-MORSE");
		public bool IsTranslatorOnline { get; private set; }

		public GpioMorseTranslator InitMorseTranslator(Gpio gpioCore) {
			if (Controller == null) {
				IsTranslatorOnline = false;
				throw new InvalidOperationException("Cannot start Morse translator as the PinController is null!");
			}

			GpioCore = gpioCore;
			IsTranslatorOnline = true;
			return this;
		}

		public async Task<bool> RelayMorseCycle(string textToConvert, int relayPin) {
			if (string.IsNullOrEmpty(textToConvert)) {
				Logger.Log("The specified text is either null or empty.", LogLevels.Warn);
				return false;
			}

			if (Controller == null) {
				Logger.Log("Malfunctioning PinController.", LogLevels.Warn);
				return false;
			}

			if (!PiController.IsValidPin(relayPin)) {
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

			GpioPinConfig beforePinStatus = Controller.GetGpioConfig(relayPin);

			if (beforePinStatus.IsPinOn) {
				Controller.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
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
						Controller.SetGpioWithTimeout(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300));
						break;
					case '-':
						Controller.SetGpioWithTimeout(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300 * 3));
						break;
					case '_':
						await Task.Delay(300).ConfigureAwait(false);
						break;
				}
			}

			GpioPinConfig afterPinStatus = Controller.GetGpioConfig(relayPin);

			if (afterPinStatus.IsPinOn) {
				Controller.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			return true;
		}
	}
}
