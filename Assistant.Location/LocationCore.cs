using Assistant.Extensions.Interfaces;
using Assistant.Logging;

namespace Assistant.Location {
	public class LocationCore : IExternal {
		public void RegisterLoggerEvent(object eventHandler) {
			LoggerExtensions.RegisterLoggerEvent(eventHandler);
		}
	}
}
