
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

// All credits for this code goes to AshV
// https://github.com/AshV

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.MorseCode {
	public class MorseCore {
		private int TimeUnitInMilliSeconds { get; set; } = 100;

		private int Frequency { get; set; } = 650;

		public char DotUnicode { get; set; } = '●'; // U+25CF

		public char DashUnicode { get; set; } = '▬'; // U+25AC

		public char Dot { get; set; } = '.';

		public char Dash { get; set; } = '-';

		private Codes CodeStore { get; set; }

		private Logger Logger = new Logger("MORSE-CORE");

		public MorseCore() => CodeStore = new Codes();

		public MorseCore(int timeUnitInMilliSeconds) : this() => TimeUnitInMilliSeconds = timeUnitInMilliSeconds;

		public string ConvertToMorseCode(string sentence, bool addStartAndEndSignal = false) {
			List<string> generatedCodeList = new List<string>();
			string[] wordsInSentence = sentence.Split(' ');

			if (addStartAndEndSignal) {
				generatedCodeList.Add(CodeStore.GetSignalCode(Codes.SignalCodes.StartingSignal));
			}

			foreach (string word in wordsInSentence) {
				foreach (char letter in word.ToUpperInvariant().ToCharArray()) {
					generatedCodeList.Add(CodeStore[letter]);
				}
				generatedCodeList.Add("_");
			}

			if (addStartAndEndSignal) {
				generatedCodeList.Add(CodeStore.GetSignalCode(Codes.SignalCodes.EndOfWork));
			}
			else {
				generatedCodeList.RemoveAt(generatedCodeList.Count - 1);
			}

			return string.Join(" ", generatedCodeList).Replace(" _ ", "  ");
		}

		public void PlayMorseTone(string morseStringOrSentence) {
			if (!Helpers.GetOsPlatform().Equals(OSPlatform.Windows)) {
				Logger.Log("Cannot play the morse tone as the OS platform is not windows.");
				return;
			}

			if (IsValidMorse(morseStringOrSentence)) {
				string pauseBetweenLetters = "_"; // One Time Unit
				string pauseBetweenWords = "_______"; // Seven Time Unit

				morseStringOrSentence = morseStringOrSentence.Replace("  ", pauseBetweenWords);
				morseStringOrSentence = morseStringOrSentence.Replace(" ", pauseBetweenLetters);

				foreach (char character in morseStringOrSentence.ToCharArray()) {
					switch (character) {
						case '.':
							Console.Beep(Frequency, TimeUnitInMilliSeconds);
							break;
						case '-':
							Console.Beep(Frequency, TimeUnitInMilliSeconds * 3);
							break;
						case '_':
							Thread.Sleep(TimeUnitInMilliSeconds);
							break;
					}
				}
			}
			else {
				PlayMorseTone(ConvertToMorseCode(morseStringOrSentence));
			}
		}

		public bool IsValidMorse(string sentence) {
			int countDot = sentence.Count(x => x == '.');
			int countDash = sentence.Count(x => x == '-');
			int countSpace = sentence.Count(x => x == ' ');

			return
				sentence.Length > (countDot + countDash + countSpace)
				? false : true;
		}
	}
}
