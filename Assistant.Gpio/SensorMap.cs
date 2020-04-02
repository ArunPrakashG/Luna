using Assistant.Gpio.Events.EventArgs;
using Newtonsoft.Json;
using System;

namespace Assistant.Gpio {
	public struct SensorMap<T> where T : ISensor {
		[JsonProperty]
		internal readonly int GpioPinNumber;

		[JsonProperty]
		internal readonly Enums.MappingEvent MapEvent;

		[JsonProperty]
		internal readonly Enums.SensorType SensorType;

		[JsonProperty]
		internal readonly Func<OnValueChangedEventArgs, bool> OnFired;

		internal SensorMap(int _gpioPinNumber, Enums.MappingEvent _mapEvent, Enums.SensorType _sensorType, Func<OnValueChangedEventArgs, bool> _onFired) {
			GpioPinNumber = _gpioPinNumber;
			MapEvent = _mapEvent;
			SensorType = _sensorType;
			OnFired = _onFired;
		}
	}
}
