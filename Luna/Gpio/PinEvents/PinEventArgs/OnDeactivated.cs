using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Gpio.PinEvents.PinEventArgs {
	internal sealed class OnDeactivatedEventArgs {
		internal readonly int Pin;
		internal readonly DateTime TimeStamp;
		internal readonly CurrentValue Current;

		internal OnDeactivatedEventArgs(int pin, CurrentValue current) {
			Pin = pin;
			TimeStamp = DateTime.Now;
			Current = current;
		}
	}
}
