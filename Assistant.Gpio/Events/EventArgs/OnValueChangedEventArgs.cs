using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events.EventArgs {
	public struct OnValueChangedEventArgs {
		public readonly int Pin;

		public readonly GpioPinState CurrentState;

		public readonly bool CurrentDigitalValue;

		public readonly GpioPinMode CurrentMode;

		public readonly DateTime EventTime;

		public readonly GpioPinState PreviousPinState;
		public readonly bool PreviousDigitalValue;

		public OnValueChangedEventArgs(int _pinNumber, GpioPinState _pinState,
			bool _pinCurrentDigitalValue, GpioPinMode _pinDriveMode,
			GpioPinState _previousPinState, bool _previousDigitalValue) {
			Pin = _pinNumber;
			CurrentState = _pinState;			
			CurrentDigitalValue = _pinCurrentDigitalValue;
			CurrentMode = _pinDriveMode;
			EventTime = DateTime.Now;
			PreviousPinState = _previousPinState;
			PreviousDigitalValue = _previousDigitalValue;
		}
	}
}
