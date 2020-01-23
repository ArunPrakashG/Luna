using Newtonsoft.Json;
using System;
using static Assistant.PushBullet.Logging.LogEnums;

namespace Assistant.PushBullet.Logging.EventArgs {
	public class LogMessageEventArgs {
		[JsonProperty]
		public string? LogMessage { get; set; }

		[JsonProperty]
		public DateTime ReceivedTime { get; set; }

		[JsonProperty]
		public LogLevel LogLevel { get; set; }

		[JsonProperty]
		public string? CallerMemberName { get; set; }

		[JsonProperty]
		public int CallerLineNumber { get; set; }

		[JsonProperty]
		public string? CallerFilePath { get; set; }

		public LogMessageEventArgs(string? msg, DateTime time, LogLevel level, string? callerName, int callerLine, string? callerFile) {
			LogMessage = msg;
			ReceivedTime = time;
			LogLevel = level;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
