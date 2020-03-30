using System;
using System.Collections.Generic;
using System.Text;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio {
	public struct SensorMapEventArgs {
		public readonly int GpioPin;
		public readonly MappingEvent Event;

		public SensorMapEventArgs(int _pin, MappingEvent _event) {
			GpioPin = _pin;
			Event = _event;
		}
	}
}
