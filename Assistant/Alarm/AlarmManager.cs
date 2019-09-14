using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Alarm {
	public class Alarm {
		[JsonProperty]
		public string AlarmMessage { get; set; }

		[JsonProperty]
		public DateTime AlarmAt { get; set; }

		[JsonProperty]
		public bool IsCompleted { get; set; }
	}

	public class AlarmManager {
		public static List<Alarm> Alarms { get; set; } = new List<Alarm>();


	}
}
