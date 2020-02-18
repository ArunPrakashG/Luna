using System;

namespace Assistant.Schedulers {
	public class Job : IJob {
		public string? Title { get; }

		public Action Task { get; }

		public TimeSpan Delay { get; }

		public Job(string? title, Action func, TimeSpan delay) {
			Title = title;
			Task = func;
			Delay = delay;
		}
	}
}
