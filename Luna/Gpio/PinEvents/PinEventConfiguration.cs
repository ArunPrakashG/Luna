using Luna.Gpio.PinEvents.PinEventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.PinEvents {
	internal class PinEventConfiguration {
		internal readonly int GpioPin;
		internal readonly CancellationTokenSource EventToken;
		internal readonly PinEventState EventState;
		internal bool IsEventRegistered;
		internal PinValueBase Current;
		internal PinValueBase Previous;

		internal PinEventConfiguration(int gpioPin, PinEventState eventState, CurrentValue currentValue, PreviousValue previousValue, CancellationTokenSource? token = default) {
			GpioPin = gpioPin;
			EventState = eventState;
			EventToken = token == null ? new CancellationTokenSource() : token;
			Current = currentValue;
			Previous = previousValue;
		}
	}
}
