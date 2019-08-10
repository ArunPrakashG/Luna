
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

using System.Collections.Generic;

namespace Assistant.MorseCode {
	public class Codes {
		public string this[char character] {
			get {
				string code = string.Empty;
				SymbolCodes.TryGetValue(character, out code);
				return code;
			}
		}

		public string GetSignalCode(SignalCodes code) {
			switch (code) {
				case SignalCodes.StartingSignal:
					return SignalMorseCodes[0];
				case SignalCodes.InvitationToTransmit:
					return SignalMorseCodes[1];
				case SignalCodes.Understood:
					return SignalMorseCodes[2];
				case SignalCodes.Error:
					return SignalMorseCodes[3];
				case SignalCodes.Wait:
					return SignalMorseCodes[4];
				case SignalCodes.EndOfWork:
					return SignalMorseCodes[5];
				default:
					return string.Empty;
			}
		}

		private static Dictionary<char, string> SymbolCodes = new Dictionary<char, string>
		{
            // Characters
            { 'A',".-" },
			{ 'B',"-..." },
			{ 'C',"-.-." },
			{ 'D',"-.." },
			{ 'E',"." },
			{ 'F',"..-." },
			{ 'G',"--" },
			{ 'H',"...." },
			{ 'I',".." },
			{ 'J',".---" },
			{ 'K',"-.-" },
			{ 'L',".-.." },
			{ 'M',"--" },
			{ 'N',"-." },
			{ 'O',"---" },
			{ 'P',".--." },
			{ 'Q',"--.-" },
			{ 'R',".-." },
			{ 'S',"..." },
			{ 'T',"-" },
			{ 'U',"..-" },
			{ 'V',"...-" },
			{ 'W',".--" },
			{ 'X',"-..-" },
			{ 'Y',"-.--" },
			{ 'Z',"--.." },

            // Numbers
            { '0',".----" },
			{ '1',"..---" },
			{ '2',"...--" },
			{ '3',"....-" },
			{ '4',"....." },
			{ '5',"-...." },
			{ '6',"--..." },
			{ '7',"---.." },
			{ '8',"----." },
			{ '9',"-----" },
           
            // Special Characters
            { '.',".-.-.-" }, // Fullstop
            { ',',"--..--" }, // Comma
            { ':',"---..." }, // Colon
            { '?',"..--.." }, // Question Mark
            { '\'',".----." }, // Apostrophe
            { '-',"-....-" }, // Hyphen, dash, minus
            { '/',"-..-." }, // Slash. division
            { '"',".-..-." }, // Quotaion mark
            { '=',"-...-" }, // Equal sign
            { '+',".-.-." }, // Plus
            { '*',"-..-" }, // multiplication
            { '@',".--.-." }, // At the rate of

            // Brackets
            { '(',"-.--." }, // Left bracket
            { '{',"-.--." }, // Left bracket
            { '[',"-.--." }, // Left bracket
            { ')',"-.--.-" }, // right bracket
            { '}',"-.--.-" }, // right bracket
            { ']',"-.--.-" }, // right bracket            
        };

		private static readonly Dictionary<int, string> SignalMorseCodes = new Dictionary<int, string> {
			{ 0,"-.-.-" },
			{ 1,"-.-" },
			{ 2,"...-." },
			{ 3,"........" },
			{ 4,".-..." },
			{ 5,"...-.-" },
		};

		public enum SignalCodes {
			StartingSignal = 0,
			InvitationToTransmit = 1,
			Understood = 2,
			Error = 3,
			Wait = 4,
			EndOfWork = 5
		}
	}
}
