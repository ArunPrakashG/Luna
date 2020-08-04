using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Gpio {
	internal class MorseCycleResult {
		internal readonly bool Status;
		internal readonly string? BaseText;
		internal readonly string? Morse;

		internal MorseCycleResult(bool _status, string? _base, string? _morse) {
			Status = _status;
			BaseText = _base;
			Morse = _morse;
		}
	}
}
