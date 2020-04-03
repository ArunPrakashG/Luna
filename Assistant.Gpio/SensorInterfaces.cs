using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Gpio {
	public interface ISensor { }
	internal class SoundSensor : ISensor { }
	internal class RelaySwitch : ISensor { }
	internal class IRSensor : ISensor { }
	internal class BuzzerModule : ISensor { }
}
