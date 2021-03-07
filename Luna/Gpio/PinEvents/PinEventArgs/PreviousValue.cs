using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.PinEvents.PinEventArgs {
	internal sealed class PreviousValue : PinValueBase {
		internal PreviousValue(GpioPinState state, bool digitalValue, GpioPinMode pinMode, PinEventState eventState) : base(state, digitalValue, pinMode, eventState) { }
	}
}
