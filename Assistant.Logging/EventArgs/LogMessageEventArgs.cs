using Newtonsoft.Json;
using System;
using static Assistant.Logging.Enums;

namespace Assistant.Logging.EventArgs {
	public class LogMessageEventArgs {
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

		public LogMessageEventArgs(string msg, DateTime time, LogLevels level, string? callerName, int callerLine, string? callerFile) {
			LogMessage = msg;
			ReceivedTime = time;
			LogLevel = level;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
