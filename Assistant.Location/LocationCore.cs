using Luna.Extensions.Interfaces;
using Luna.Logging;

namespace Luna.Location {
	public class LocationCore : IExternal {
		public void RegisterLoggerEvent(object eventHandler) {
			LoggerExtensions.RegisterLoggerEvent(eventHandler);
		}
	}
}
