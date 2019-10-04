using Assistant.Extensions;
using Newtonsoft.Json;
using System;

namespace Assistant.Alarm {
	public class AlarmConfig {
		[JsonProperty]
		public string? AlarmMessage { get; set; }

		[JsonProperty]
		public DateTime AlarmAt { get; set; }

		[JsonIgnore]
		public TimeSpan AlarmSpan => AlarmAt - DateTime.Now;

		[JsonProperty]
		public string? AlarmGuid { get; set; }

		[JsonProperty]
		public bool ShouldRepeat { get; set; }

		[JsonProperty]
		public bool ShouldUseTTS { get; set; }

		[JsonProperty]
		public bool ShouldOverideSoundSetting { get; set; }

		[JsonProperty]
		public bool Snooze { get; set; }

		[JsonIgnore]
		public Scheduler? Scheduler { get; set; } = new Scheduler();
	}
}
