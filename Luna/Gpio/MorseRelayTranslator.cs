using Luna.Features.Morse;
using Luna.Gpio.Controllers;
using Luna.Logging;
using System;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Gpio {
	internal class MorseRelayTranslator {
		private readonly InternalLogger Logger = new InternalLogger(nameof(MorseRelayTranslator));
		private readonly GpioCore Controller;

		internal MorseRelayTranslator(GpioCore gpioCore) {
			Controller = gpioCore ?? throw new ArgumentNullException(nameof(gpioCore));
		}

		internal async Task<MorseCycleResult> RelayMorseCycle(string textToConvert, int relayPin) {
			if (string.IsNullOrEmpty(textToConvert)) {
				return new MorseCycleResult(false, null, null);
			}

			if (PinController.GetDriver() == null) {
				Logger.Warn("Driver isn't started yet.");
				return new MorseCycleResult(false, null, null);
			}

			Logger.Trace($"Converting to Morse...");
			string morse = MorseCore.ConvertToMorseCode(textToConvert);

			if (string.IsNullOrEmpty(morse)) {
				Logger.Warn("Conversion to Morse failed. Cannot proceed.");
				return new MorseCycleResult(false, null, null);
			}

			Logger.Trace($"TEXT >> {textToConvert}");
			Logger.Trace($"MORSE >> {morse}");

			Pin beforePinStatus = PinController.GetDriver().GetPinConfig(relayPin);

			if (beforePinStatus.IsPinOn) {
				PinController.GetDriver().SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			if (!MorseCore.IsValidMorse(morse)) {
				Logger.Warn("The specified Morse is invalid!");
				return new MorseCycleResult(false, null, null);
			}

			string pauseBetweenLetters = "_";     // One Time Unit
			string pauseBetweenWords = "_______"; // Seven Time Unit

			morse = morse.Replace("  ", pauseBetweenWords);
			morse = morse.Replace(" ", pauseBetweenLetters);

			char[] morseCharArray = morse.ToCharArray();

			for (int i = 0; i < morseCharArray.Length; i++) {
				char charecter = morseCharArray[i];

				switch (charecter) {
					case '.':
						PinController.GetDriver().SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300), true);
						continue;
					case '-':
						PinController.GetDriver().SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300 * 3), true);
						continue;
					case '_':
						await Task.Delay(300).ConfigureAwait(false);
						continue;
				}
			}

			Pin afterPinStatus = PinController.GetDriver().GetPinConfig(relayPin);

			if (afterPinStatus.IsPinOn) {
				PinController.GetDriver().SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			return new MorseCycleResult(false, textToConvert, morse);
		}
	}
}
