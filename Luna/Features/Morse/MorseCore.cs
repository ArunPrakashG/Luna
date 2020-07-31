// All credits for this code goes to AshV
// https://github.com/AshV

using Luna.ExternalExtensions;
using Luna.ExternalExtensions.Interfaces;
using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Luna.Features.Morse {
	internal static class MorseCore {
		private static readonly InternalLogger Logger = new InternalLogger(nameof(MorseCore));
		private const int TimeUnitInMilliSeconds = 100;
		private const int Frequency  = 650;
		private const char DotUnicode = '•'; // U+25CF
		private const char DashUnicode = '▬'; // U+25AC
		private const char Dot = '.';
		private const char Dash = '-';
		private static readonly Codes CodeStore = new Codes();

		internal static string ConvertToMorseCode(string sentence, bool addStartAndEndSignal = false) {
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

		internal static void PlayMorseTone(string morseStringOrSentence) {
			if (!Helpers.GetPlatform().Equals(OSPlatform.Windows)) {
				Logger.Warn("Cannot play the Morse tone as the OS platform is not windows.");
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

		internal static bool IsValidMorse(string sentence) {
			int countDot = sentence.Count(x => x == '.');
			int countDash = sentence.Count(x => x == '-');
			int countSpace = sentence.Count(x => x == ' ');
			return sentence.Length > (countDot + countDash + countSpace) ? false : true;
		}
	}
}
