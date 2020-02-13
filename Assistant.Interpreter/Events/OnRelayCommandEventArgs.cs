using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Interpreter.Events {
	public class OnRelayCommandEventArgs {
		public readonly int RelayPin;
		public readonly bool PinOn;
		public readonly int DelayBeforeSet;

		public OnRelayCommandEventArgs(int pin, bool isOn, int delayBeforeSet = 0) {
			RelayPin = pin;
			PinOn = isOn;
			DelayBeforeSet = delayBeforeSet;
		}
	}
}
