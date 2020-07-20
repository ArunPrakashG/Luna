using Luna.Gpio.Drivers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Gpio.Exceptions {
	public class DriverNotInitializedException : Exception {
		public DriverNotInitializedException()
			: base("The specified " + nameof(IGpioControllerDriver) + " failed to initialize.") {

		}

		public DriverNotInitializedException(string driverName)
			: base(string.Format("The specified driver {0} isn't initialized yet.", driverName)) {

		}
	}
}
