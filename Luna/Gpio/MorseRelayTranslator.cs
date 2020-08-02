using Luna.Gpio.Config;
using Luna.Gpio.Controllers;
using Luna.Gpio.Drivers;
using Luna.Logging;
using Luna.Logging.Interfaces;
using Luna.Morse;
using System;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;
using static Luna.Logging.Enums;

namespace Luna.Gpio {
	public struct MorseCycleResult {
		internal readonly bool Status;
		internal readonly string? BaseText;
		internal readonly string? Morse;

		public MorseCycleResult(bool _status, string? _base, string? _morse) {
			Status = _status;
			BaseText = _base;
			Morse = _morse;
		}
	}

	public class MorseRelayTranslator {
		private readonly ILogger Logger = new Logger(typeof(MorseRelayTranslator).Name);
		private static readonly MorseCore MorseCore = new MorseCore();
		private readonly GpioCore Controller;
		private GpioControllerDriver? Driver => PinController.GetDriver();

		internal MorseRelayTranslator(GpioCore _controller) => Controller = _controller;

		public static MorseCore GetCore() => MorseCore;

		public async Task<MorseCycleResult> RelayMorseCycle(string textToConvert, int relayPin) {
			if (string.IsNullOrEmpty(textToConvert)) {
				Logger.Log("The specified text is empty.", LogLevels.Warn);
				return new MorseCycleResult(false, null, null);
			}
			
			if (Driver == null) {
				Logger.Log("Driver isn't started yet.", LogLevels.Warn);
				return new MorseCycleResult(false, null, null);
			}

			Logger.Trace($"Converting to Morse...");
			string morse = MorseCore.ConvertToMorseCode(textToConvert);

			if (string.IsNullOrEmpty(morse)) {
				Logger.Log("Conversion to Morse failed. Cannot proceed.", LogLevels.Warn);
				return new MorseCycleResult(false, null, null);
			}

			Logger.Trace($"TEXT >> {textToConvert}");
			Logger.Trace($"MORSE >> {morse}");

			Pin beforePinStatus = Driver.GetPinConfig(relayPin);

			if (beforePinStatus.IsPinOn) {
				Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			if (!MorseCore.IsValidMorse(morse)) {
				Logger.Log("The specified Morse is invalid!", LogLevels.Warn);
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
						Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300), true);
						continue;
					case '-':
						Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.On, TimeSpan.FromMilliseconds(300 * 3), true);
						continue;
					case '_':
						await Task.Delay(300).ConfigureAwait(false);
						continue;
				}
			}

			Pin afterPinStatus = Driver.GetPinConfig(relayPin);

			if (afterPinStatus.IsPinOn) {
				Driver.SetGpioValue(relayPin, GpioPinMode.Output, GpioPinState.Off);
			}

			return new MorseCycleResult(false, textToConvert, morse);
		}
	}
}
