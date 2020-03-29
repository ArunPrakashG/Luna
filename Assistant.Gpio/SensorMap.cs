using Assistant.Gpio.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Assistant.Gpio {
	public struct SensorMap<T> where T : ISensor {
		[JsonProperty]
		public readonly int GpioPinNumber;

		[JsonProperty]
		public readonly Enums.MappingEvent MapEvent;

		public SensorMap(int _gpioPinNumber, Enums.MappingEvent _mapEvent) {
			GpioPinNumber = _gpioPinNumber;
			MapEvent = _mapEvent;
		}
	}

	public class SensorMapCollection : List<SensorMap<ISensor>> { }
}
