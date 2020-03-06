using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events.EventArgs {
	public struct OnValueChangedEventArgs {
		public readonly int Pin;

		public readonly GpioPinState State;

		public readonly bool DigitalValue;

		public readonly GpioPinMode Mode;

		public readonly DateTime EventTime;

		public OnValueChangedEventArgs(int _pinNumber, GpioPinState _pinState,
			bool _pinCurrentDigitalValue,
			GpioPinMode _pinDriveMode) {
			Pin = _pinNumber;
			State = _pinState;			
			DigitalValue = _pinCurrentDigitalValue;
			Mode = _pinDriveMode;
			EventTime = DateTime.Now;
		}
	}
}
