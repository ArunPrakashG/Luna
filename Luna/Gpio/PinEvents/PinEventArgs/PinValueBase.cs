using System;
using System.Collections.Generic;
using System.Text;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.PinEvents.PinEventArgs {
	internal abstract class PinValueBase {
		internal readonly GpioPinState State;
		internal readonly bool DigitalValue;
		internal readonly GpioPinMode PinMode;
		internal readonly PinEventStates EventState;

		internal PinValueBase(GpioPinState state, bool digitalValue, GpioPinMode pinMode, PinEventStates eventState) {
			State = state;
			DigitalValue = digitalValue;
			PinMode = pinMode;
			EventState = eventState;
		}
	}
}
