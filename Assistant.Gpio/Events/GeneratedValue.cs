using System;
using System.Collections.Generic;
using System.Text;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Events {
	internal struct GeneratedValue {
		internal GpioPinState PinState { get; private set; }
		internal bool DigitalValue { get; private set; }

		internal GeneratedValue(GpioPinState _state, bool _digitalValue) {
			PinState = _state;
			DigitalValue = _digitalValue;
		}

		internal void SetState(GpioPinState _state) {
			PinState = _state;
		}

		internal void SetDigitalValue(bool _digitalValue) {
			DigitalValue = _digitalValue;
		}

		internal void Set(GpioPinState _state, bool _digitalValue) {
			PinState = _state;
			DigitalValue = _digitalValue;
		}
	}
}
