using Newtonsoft.Json;
using System;

namespace Luna.Logging.EventArgs {
	public class EventArgsBase {
		[JsonProperty]
		public string? LogIdentifier { get; private set; }

		[JsonProperty]
		public DateTime LogTime { get; private set; }

		[JsonProperty]
		public string? LogMessage { get; private set; }

		[JsonProperty]
		public string? CallerMemberName { get; private set; }

		[JsonProperty]
		public int CallerLineNumber { get; private set; }

		[JsonProperty]
		public string? CallerFilePath { get; private set; }

		public EventArgsBase(string? logId, DateTime dt, string? msg, string? callerName, int callerLine, string? callerFile) {
			LogIdentifier = logId;
			LogTime = dt;
			LogMessage = msg;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
