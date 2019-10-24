using AssistantSharedLibrary.Logging.EventArgs;
using System;
using System.Runtime.CompilerServices;

namespace AssistantSharedLibrary.Logging {
	public static class EventLogger {
		public delegate void OnLogMessageReceived(object sender, LogMessageEventArgs e);
		public static event OnLogMessageReceived LogMessageReceived;

		public delegate void OnWarningLogMessageReceived(object sender, EventArgsBase e);
		public static event OnWarningLogMessageReceived OnWarningOccured;

		public delegate void OnErrorLogMessageReceived(object sender, EventArgsBase e);
		public static event OnErrorLogMessageReceived OnErrorOccured;

		public delegate void OnExceptionLogMessageRecevied(object sender, OnExceptionMessageEventArgs e);
		public static event OnExceptionLogMessageRecevied OnExceptionOccured;

		public static string LogHeader { get; set; } = " ";

		public static void LogDebug(string msg, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(null, new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.DEBUG, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogInfo(string msg, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(null, new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.INFO, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogTrace(string msg, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(null, new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.TRACE, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogWarning(string msg, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(null, new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.WARN, previousMethodName, callermemberlineNo, calledFilePath));
			OnWarningOccured?.Invoke(null, new EventArgsBase(DateTime.Now, msg, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogError(string msg, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(null, new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.ERROR, previousMethodName, callermemberlineNo, calledFilePath));
			OnErrorOccured?.Invoke(null, new EventArgsBase(DateTime.Now, msg, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogException(Exception e, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			if (e == null) {
				return;
			}

			LogMessageReceived?.Invoke(null, new LogMessageEventArgs(e.ToString(), DateTime.Now, LogEnums.LogLevel.EXCEPTION, previousMethodName, callermemberlineNo, calledFilePath));
			OnExceptionOccured?.Invoke(null, new OnExceptionMessageEventArgs(e, DateTime.Now, previousMethodName, callermemberlineNo, calledFilePath));
		}
	}
}
