using Newtonsoft.Json;
using System;

namespace Assistant.Logging.EventArgs {
	public class OnExceptionMessageEventArgs {
		[JsonProperty]
		public string? LogIdentifier { get; private set; }

		[JsonProperty]
		public Exception? LogException { get; private set; }

		[JsonProperty]
		public DateTime LogTime { get; private set; }

		[JsonProperty]
		public string? CallerMemberName { get; private set; }

		[JsonProperty]
		public int CallerLineNumber { get; private set; }

		[JsonProperty]
		public string? CallerFilePath { get; private set; }

		public OnExceptionMessageEventArgs(string? logId, Exception e, DateTime dt, string? callerName, int callerLine, string? callerFile) {
			LogIdentifier = logId;
			LogException = e;
			LogTime = dt;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
