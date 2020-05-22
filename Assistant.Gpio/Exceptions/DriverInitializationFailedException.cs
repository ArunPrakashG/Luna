using System;

namespace Assistant.Gpio.Exceptions {
	public class DriverInitializationFailedException : Exception {
		public DriverInitializationFailedException(string driverName)
			: base(string.Format("A driver failed to initialize properly. {0}", driverName)) { }

		public DriverInitializationFailedException(string driverName, string reason)
			: base(string.Format("'{0}' driver failed to initialize properly due to {1}", driverName, reason)) { }
	}
}
