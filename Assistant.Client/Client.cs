using Assistant.Extensions.Interfaces;
using Assistant.Logging;

namespace Assistant.Client {
	public class Client : IExternal {
		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
