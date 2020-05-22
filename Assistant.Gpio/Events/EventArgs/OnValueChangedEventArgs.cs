using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events.EventArgs {
	internal struct OnValueChangedEventArgs {
		internal readonly int Pin;
		internal readonly GpioPinState CurrentState;
		internal readonly bool CurrentDigitalValue;
		internal readonly GpioPinMode CurrentMode;
		internal readonly DateTime TimeStamp;
		internal readonly GpioPinState PreviousPinState;
		internal readonly PinEventStates CurrentEventState;
		internal readonly bool PreviousDigitalValue;

		internal OnValueChangedEventArgs(int _pinNumber, GpioPinState _pinState,
			bool _pinCurrentDigitalValue, GpioPinMode _pinDriveMode, PinEventStates _state,
			GpioPinState _previousPinState, bool _previousDigitalValue) {
			Pin = _pinNumber;
			CurrentState = _pinState;
			CurrentDigitalValue = _pinCurrentDigitalValue;
			CurrentMode = _pinDriveMode;
			TimeStamp = DateTime.Now;
			PreviousPinState = _previousPinState;
			PreviousDigitalValue = _previousDigitalValue;
			CurrentEventState = _state;
		}
	}
}
