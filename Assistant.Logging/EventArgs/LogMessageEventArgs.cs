using Newtonsoft.Json;
using System;
using static Luna.Logging.Enums;

namespace Luna.Logging.EventArgs {
	public class LogMessageEventArgs {
		[JsonProperty]
		public string? LogIdentifier { get; private set; }

		[JsonProperty]
		public string? LogMessage { get; private set; }

		[JsonProperty]
		public DateTime ReceivedTime { get; private set; }

		[JsonProperty]
		public LogLevels LogLevel { get; private set; }

		[JsonProperty]
		public string? CallerMemberName { get; private set; }

		[JsonProperty]
		public int CallerLineNumber { get; private set; }

		[JsonProperty]
		public string? CallerFilePath { get; private set; }

		public LogMessageEventArgs(string? logId, string? msg, DateTime time, LogLevels level, string? callerName, int callerLine, string? callerFile) {
			LogIdentifier = logId;
			LogMessage = msg;
			ReceivedTime = time;
			LogLevel = level;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
