using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Logging.EventArgs {
	public class OnExceptionMessageEventArgs {
		[JsonProperty]
		public Exception LogException { get; set; }

		[JsonProperty]
		public DateTime LogTime { get; set; }

		[JsonProperty]
		public string CallerMemberName { get; set; }

		[JsonProperty]
		public int CallerLineNumber { get; set; }

		[JsonProperty]
		public string CallerFilePath { get; set; }

		public OnExceptionMessageEventArgs(Exception e, DateTime dt, string callerName, int callerLine, string callerFile) {
			LogException = e;
			LogTime = dt;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
