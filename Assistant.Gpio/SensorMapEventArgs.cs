using System;
using System.Collections.Generic;
using System.Text;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio {
	internal struct SensorMapEventArgs {
		internal readonly int GpioPin;
		internal readonly MappingEvent Event;

		internal SensorMapEventArgs(int _pin, MappingEvent _event) {
			GpioPin = _pin;
			Event = _event;
		}
	}
}
