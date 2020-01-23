using Newtonsoft.Json;
using System;

namespace Assistant.PushBullet.Logging.EventArgs {
	public class EventArgsBase {
		[JsonProperty]
		public DateTime LogTime { get; set; }

		[JsonProperty]
		public string? LogMessage { get; set; }

		[JsonProperty]
		public string? CallerMemberName { get; set; }

		[JsonProperty]
		public int CallerLineNumber { get; set; }

		[JsonProperty]
		public string? CallerFilePath { get; set; }

		public EventArgsBase(DateTime dt, string? msg, string? callerName, int callerLine, string? callerFile) {
			LogTime = dt;
			LogMessage = msg;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
