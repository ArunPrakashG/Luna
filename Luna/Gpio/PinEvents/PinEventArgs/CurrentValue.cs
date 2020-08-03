using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Gpio.PinEvents.PinEventArgs {
	internal class CurrentValue : PinValueBase {
		internal CurrentValue(Enums.GpioPinState state, bool digitalValue, Enums.GpioPinMode pinMode, Enums.PinEventStates eventState) : base(state, digitalValue, pinMode, eventState) {	}
	}
}
