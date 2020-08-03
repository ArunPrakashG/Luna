using System;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.PinEvents.PinEventArgs {
	internal sealed class OnValueChangedEventArgs {
		internal readonly int Pin;
		internal readonly DateTime TimeStamp;
		internal readonly CurrentValue Current;
		internal readonly PreviousValue Previous;

		internal OnValueChangedEventArgs(int pinNumber, CurrentValue current, PreviousValue previous) {
			Pin = pinNumber;
			TimeStamp = DateTime.Now;
			Current = current ?? throw new ArgumentNullException(nameof(CurrentValue));
			Previous = previous ?? throw new ArgumentNullException(nameof(PreviousValue));
		}
	}
}
