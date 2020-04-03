using Assistant.Gpio.Events.EventArgs;
using Newtonsoft.Json;
using System;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio {
	public struct PinMap {
		[JsonProperty]
		internal readonly int GpioPinNumber;

		[JsonProperty]
		internal readonly MappingEvent MapEvent;

		[JsonProperty]
		internal readonly SensorType PinType;

		[JsonProperty]
		internal readonly Func<OnValueChangedEventArgs, bool> OnFired;

		internal PinMap(int _gpioPinNumber, MappingEvent _mapEvent, SensorType _sensorType, Func<OnValueChangedEventArgs, bool> _onFired) {
			GpioPinNumber = _gpioPinNumber;
			MapEvent = _mapEvent;
			PinType = _sensorType;
			OnFired = _onFired;
		}
	}
}
