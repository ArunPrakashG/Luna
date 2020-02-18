using System;

namespace Assistant.Schedulers {
	public interface IJob {
		string? Title { get; }
		Action Task { get; }
		TimeSpan Delay { get; }
	}
}
