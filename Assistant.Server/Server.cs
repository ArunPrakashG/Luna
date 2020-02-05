using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server {
	public class Server : IExternal {
		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
