using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using FluentScheduler;

namespace Assistant.Schedulers {
	public class SchedulersCore : IExternal {
		private static readonly Registry JobRegistry = new Registry();

		public SchedulersCore() => JobManager.Initialize(JobRegistry);

		internal Registry GetJobRegistry() => JobRegistry;

		public void RegisterLoggerEvent(object eventHandler) {
			LoggerExtensions.RegisterLoggerEvent(eventHandler);
		}
	}
}
