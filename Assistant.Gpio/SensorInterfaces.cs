using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Gpio {
	public interface ISensor { }
	internal interface ISoundSensor : ISensor { }
	internal interface IRelaySwitch : ISensor { }
	internal interface IIRSensor : ISensor { }
	internal interface IBuzzer : ISensor { }
}
