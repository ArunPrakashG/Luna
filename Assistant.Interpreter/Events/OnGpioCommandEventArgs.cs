using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Interpreter.Events {
	public class OnGpioCommandEventArgs {
		public readonly int GpioPin;
		public readonly bool PinOn;

		public OnGpioCommandEventArgs(int pin, bool isOn) {
			GpioPin = pin;
			PinOn = isOn;
		}
	}
}
