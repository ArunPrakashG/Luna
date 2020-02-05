// All credits for this code goes to AshV
// https://github.com/AshV

using System.Collections.Generic;

namespace Assistant.Morse {
	public class Codes {
		public string this[char character] {
			get {
				SymbolCodes.TryGetValue(character, out string? code);
				return code ?? string.Empty;
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
