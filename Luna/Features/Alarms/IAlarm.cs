using System;

namespace Luna.Features.Alarms {
	public interface IAlarm {
		string? Name { get; }
		string? Description { get; }
		Action<IAlarm>? Task { get; }
		DateTime At { get; }
		TimeSpan TimeSpanAt { get; }
		string? Id { get; }
		bool ShouldRepeat { get; }
		bool ShouldSpeakOutLoud { get; }
	}
}
