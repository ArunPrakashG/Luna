using Assistant.Gpio.Events.EventArgs;
using Newtonsoft.Json;
using System;

namespace Assistant.Gpio {
	public struct SensorMap<T> where T : ISensor {
		[JsonProperty]
		public readonly int GpioPinNumber;

		[JsonProperty]
		public readonly Enums.MappingEvent MapEvent;

		[JsonProperty]
		public readonly Enums.SensorType SensorType;

		[JsonProperty]
		public readonly Func<OnValueChangedEventArgs, bool> OnFired;

		public SensorMap(int _gpioPinNumber, Enums.MappingEvent _mapEvent, Enums.SensorType _sensorType, Func<OnValueChangedEventArgs, bool> _onFired) {
			GpioPinNumber = _gpioPinNumber;
			MapEvent = _mapEvent;
			SensorType = _sensorType;
			OnFired = _onFired;
		}
	}
}
