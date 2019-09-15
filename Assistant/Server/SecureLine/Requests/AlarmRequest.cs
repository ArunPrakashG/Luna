using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Assistant.Server.SecureLine.Requests {
	public class AlarmRequest {
		[JsonProperty]
		public int HoursFromNow { get; set; }

		[JsonProperty]
		public string AlarmMessage { get; set; }

		[JsonProperty]
		public bool UseTTS { get; set; }

		[JsonProperty]
		public bool Repeat { get; set; } = false;

		[JsonProperty]
		public int RepeatHours { get; set; } = 0;
	}
}
