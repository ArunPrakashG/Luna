using FluentScheduler;
using System;

namespace Luna.Features.Alarms {
	public class Alarm : IAlarm, IJob {
		public string? Name { get; }

		public string? Description { get; }

		public Action<IAlarm>? Task { get; }

		public DateTime At { get; }

		public string? Id { get; }

		public bool ShouldRepeat { get; }

		public bool ShouldSpeakOutLoud { get; }

		public TimeSpan TimeSpanAt { get; }

		public Alarm() { }

		public Alarm(string? name, string? desc, Action<IAlarm>? task, DateTime at, bool shouldRepeat, bool useTts) {
			Name = name;
			Description = desc;
			Task = task;
			At = at;
			TimeSpanAt = at - DateTime.Now;
			Id = Guid.NewGuid().ToString();
			ShouldRepeat = shouldRepeat;
			ShouldSpeakOutLoud = useTts;
		}

		public Alarm(string? name, string? desc, Action<IAlarm>? task, TimeSpan at, bool shouldRepeat, bool useTts) {
			Name = name;
			Description = desc;
			Task = task;
			At = DateTime.Parse(at.ToString());
			TimeSpanAt = at;
			Id = Guid.NewGuid().ToString();
			ShouldRepeat = shouldRepeat;
			ShouldSpeakOutLoud = useTts;
		}

		public void Execute() {
			if(Task != null) {
				Task.Invoke(this);
			}
		}
	}
}
