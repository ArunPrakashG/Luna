using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Gpio {
	public interface ISensor { }
	public interface ISoundSensor : ISensor { }
	public interface IRelaySwitch : ISensor { }
	public interface IIRSensor : ISensor { }
	public interface IBuzzer : ISensor { }
}
