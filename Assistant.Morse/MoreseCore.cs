// All credits for this code goes to AshV
// https://github.com/AshV

using Assistant.Extensions;
using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Assistant.Morse {
	public class MorseCore : IExternal {
		private int TimeUnitInMilliSeconds { get; set; } = 100;

		private int Frequency { get; set; } = 650;

		public char DotUnicode { get; set; } = '●'; // U+25CF

		public char DashUnicode { get; set; } = '▬'; // U+25AC

		public char Dot { get; set; } = '.';

		public char Dash { get; set; } = '-';

		private Codes CodeStore { get; set; }

		private ILogger Logger = new Logger("MORSE-CORE");

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
				Logger.Log("Cannot play the Morse tone as the OS platform is not windows.");
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

		public void RegisterLoggerEvent(object? eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
