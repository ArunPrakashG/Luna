using Newtonsoft.Json;

namespace Assistant.Servers.SecureLine.Requests {
	public class AlarmRequest {
		[JsonProperty]
		public int HoursFromNow { get; set; }

		[JsonProperty]
		public string AlarmMessage { get; set; } = string.Empty;

		[JsonProperty]
		public bool UseTTS { get; set; }

		[JsonProperty]
		public bool Repeat { get; set; } = false;

		[JsonProperty]
		public int RepeatHours { get; set; } = 0;
	}
}
