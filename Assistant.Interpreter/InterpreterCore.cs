using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.Interpreter
{
	public class InterpreterCore : IExternal
	{
		internal static readonly ILogger Logger = new Logger("INTERPRETER-CORE");

		public InterpreterCore() { }

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
