using Newtonsoft.Json;
using System;

namespace Assistant.Logging.EventArgs {
	public class EventArgsBase {
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

		public EventArgsBase(DateTime dt, string? msg, string? callerName, int callerLine, string? callerFile) {
			LogTime = dt;
			LogMessage = msg;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
