// All credits for this code goes to AshV
// https://github.com/AshV

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Assistant.Extensions;

namespace Assistant.MorseCode {
	public class MorseCore {
		private int TimeUnitInMilliSeconds { get; set; } = 100;

		private int Frequency { get; set; } = 650;

		public char DotUnicode { get; set; } = '●'; // U+25CF

		public char DashUnicode { get; set; } = '▬'; // U+25AC

		public char Dot { get; set; } = '.';

		public char Dash { get; set; } = '-';

		private Codes CodeStore { get; set; }

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
