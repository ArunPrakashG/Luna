
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

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
		private PiController Controller { get; set; }
		private readonly Logger Logger = new Logger("GPIO-MORSE");
		public bool IsTranslatorOnline { get; private set; }

		public GpioMorseTranslator (PiController controller) {
			if (controller != null) {
				Controller = controller;
				IsTranslatorOnline = true;
			}
			else {
				Logger.Log("Cannot start morse translator for gpio pins as the controller is null.", Enums.LogLevels.Warn);
				IsTranslatorOnline = false;
			}

		}

		public async Task<bool> RelayMorseCycle(string textToConvert, int relayPin) {
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
						GpioPinConfig pinStatus = Controller.GetPinConfig(pin);

						if (pinStatus.PinValue == GpioPinValue.Low) {
							Controller.SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
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
						Controller.SetGpioWithTimeout(relayPin, GpioPinDriveMode.Output, GpioPinValue.Low, TimeSpan.FromMilliseconds(300));
						break;
					case '-':
						Controller.SetGpioWithTimeout(relayPin, GpioPinDriveMode.Output, GpioPinValue.Low, TimeSpan.FromMilliseconds(300 * 3));
						break;
					case '_':
						await Task.Delay(300).ConfigureAwait(false);
						break;
				}
			}

			if (Core.Config.RelayPins != null && Core.Config.RelayPins.Count() > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinConfig pinStatus = Controller.GetPinConfig(pin);

					if (pinStatus.PinValue == GpioPinValue.Low) {
						Controller.SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			return true;
		}
	}
}
