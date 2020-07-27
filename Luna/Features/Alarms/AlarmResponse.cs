using System;

namespace Luna.Features.Alarms
{
	public struct AlarmResponse {
		public bool IsScheduled { get; }
		public string? Id { get; }
		public DateTime ScheduledAt { get; }

		public AlarmResponse(bool isSet, string? id, DateTime schAt) {
			IsScheduled = isSet;
			Id = id;
			ScheduledAt = schAt;
		}
	}
}
