using Assistant.PushBullet.Logging.EventArgs;
using System;
using System.Runtime.CompilerServices;

namespace Assistant.PushBullet.Logging {
	public static class EventLogger {
		public delegate void OnLogMessageReceived(object sender, LogMessageEventArgs e);
		public static event OnLogMessageReceived? LogMessageReceived;

		public delegate void OnWarningLogMessageReceived(object sender, EventArgsBase e);
		public static event OnWarningLogMessageReceived? OnWarningOccured;

		public delegate void OnErrorLogMessageReceived(object sender, EventArgsBase e);
		public static event OnErrorLogMessageReceived? OnErrorOccured;

		public delegate void OnExceptionLogMessageRecevied(object sender, OnExceptionMessageEventArgs e);
		public static event OnExceptionLogMessageRecevied? OnExceptionOccured;

		public static string LogHeader { get; set; } = " ";

		public static void Debug(this string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) => LogDebug(msg, previousMethodName, callermemberlineNo, calledFilePath);

		public static void Info(this string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) => LogInfo(msg, previousMethodName, callermemberlineNo, calledFilePath);

		public static void Trace(this string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) => LogTrace(msg, previousMethodName, callermemberlineNo, calledFilePath);

		public static void Warn(this string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) => LogWarning(msg, previousMethodName, callermemberlineNo, calledFilePath);

		public static void Error(this string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) => LogError(msg, previousMethodName, callermemberlineNo, calledFilePath);

		public static void Exception(this Exception e, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) => LogException(e, previousMethodName, callermemberlineNo, calledFilePath);

		public static void LogDebug(string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(new object(), new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.DEBUG, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogInfo(string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(new object(), new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.INFO, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogTrace(string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(new object(), new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.TRACE, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogWarning(string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(new object(), new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.WARN, previousMethodName, callermemberlineNo, calledFilePath));
			OnWarningOccured?.Invoke(new object(), new EventArgsBase(DateTime.Now, msg, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogError(string msg, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
				return;
			}

			LogMessageReceived?.Invoke(new object(), new LogMessageEventArgs(msg, DateTime.Now, LogEnums.LogLevel.ERROR, previousMethodName, callermemberlineNo, calledFilePath));
			OnErrorOccured?.Invoke(new object(), new EventArgsBase(DateTime.Now, msg, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public static void LogException(Exception e, [CallerMemberName] string? previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string? calledFilePath = null) {
			if (e == null) {
				return;
			}

			LogMessageReceived?.Invoke(new object(), new LogMessageEventArgs(e.ToString(), DateTime.Now, LogEnums.LogLevel.EXCEPTION, previousMethodName, callermemberlineNo, calledFilePath));
			OnExceptionOccured?.Invoke(new object(), new OnExceptionMessageEventArgs(e, DateTime.Now, previousMethodName, callermemberlineNo, calledFilePath));
		}
	}
}
