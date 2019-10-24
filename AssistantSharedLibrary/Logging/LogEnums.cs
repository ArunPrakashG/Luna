using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Logging {
	public static class LogEnums {
		public enum LogLevel {
			TRACE,
			DEBUG,
			INFO,
			WARN,
			ERROR,
			EXCEPTION,
			FATAL,
			CUSTOM
		}
	}
}
