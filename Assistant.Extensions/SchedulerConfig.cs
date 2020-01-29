using System;
using System.Collections.Generic;

namespace Assistant.Extensions {
	public class SchedulerConfig {
		public TimeSpan ScheduledSpan { get; set; }
		public string? Guid { get; set; }
		public TimeSpan RepeatInterval { get; set; }
		public List<object> SchedulerObjects { get; set; } = new List<object>();
	}
}
