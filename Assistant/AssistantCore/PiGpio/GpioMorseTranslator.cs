using Assistant.Extensions;
using Assistant.Log;
using Assistant.MorseCode;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.AssistantCore.PiGpio {
	public class GpioMorseTranslator {
		private MorseCore MorseCore => Core.MorseCode;
		private GPIOController Controller { get; set; }
		private readonly Logger Logger = new Logger("GPIO-MORSE");
		public bool IsTranslatorOnline { get; private set; }

		public GpioMorseTranslator (GPIOController controller) {
			if (controller != null) {
				Controller = controller;
				IsTranslatorOnline = true;
			}
			else {
				Logger.Log("Cannot start morse translator for gpio pins as the controller is null.", Enums.LogLevels.Warn);
				IsTranslatorOnline = false;
			}

		}

		public async Task<bool> RelayMorseCycle(string textToConvert, int relayPin, int timeout = 300) {
			if (Helpers.IsNullOrEmpty(textToConvert)) {
				Logger.Log("The specified text is either null or empty.", Enums.LogLevels.Warn);
				return false;
			}

			if (relayPin <= 0) {
				Logger.Log("Please specify a valid relay pin to run the cycle.", Enums.LogLevels.Warn);
				return false;
			}

			Logger.Log($"Converting {textToConvert} to morse...", Enums.LogLevels.Trace);
			string Morse = MorseCore.ConvertToMorseCode(textToConvert);

			if (Helpers.IsNullOrEmpty(Morse)) {
				Logger.Log("Conversion to morse failed. cannot proceed.", Enums.LogLevels.Warn);
				return false;
			}

			Logger.Log($"TEXT >> {textToConvert}");
			Logger.Log($"MORSE >> {Morse}");

			if (Core.Config.RelayPins != null && Core.Config.RelayPins.Count() > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					if (pin.Equals(relayPin)) {
						GpioPinConfig pinStatus = Controller.FetchPinStatus(pin);

						if (pinStatus.IsOn) {
							Controller.SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
						}

						break;
					}
				}
			}

			if (!MorseCore.IsValidMorse(Morse)) {
				Logger.Log("The specified morse is not valid!", Enums.LogLevels.Warn);
				return false;
			}

			string pauseBetweenLetters = "_";     // One Time Unit
			string pauseBetweenWords = "_______"; // Seven Time Unit

			Morse = Morse.Replace("  ", pauseBetweenWords);
			Morse = Morse.Replace(" ", pauseBetweenLetters);

			foreach (char character in Morse.ToCharArray()) {
				switch (character) {
					case '.':
						await Controller.SetGPIO(relayPin, GpioPinDriveMode.Output, GpioPinValue.Low, timeout).ConfigureAwait(false);
						break;
					case '-':
						await Controller.SetGPIO(relayPin, GpioPinDriveMode.Output, GpioPinValue.Low, timeout * 3).ConfigureAwait(false);
						break;
					case '_':
						await Task.Delay(timeout).ConfigureAwait(false);
						break;
				}
			}

			if (Core.Config.RelayPins != null && Core.Config.RelayPins.Count() > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinConfig pinStatus = Controller.FetchPinStatus(pin);

					if (pinStatus.IsOn) {
						Controller.SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			return true;
		}
	}
}
