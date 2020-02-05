using Assistant.Logging.Interfaces;
using System;
using System.Runtime.CompilerServices;
using static Assistant.Logging.Enums;
using static Assistant.Logging.Logger;

namespace Assistant.Logging {
	public static class LoggerExtensions {
		public static void RegisterLoggerEvent(object? eventHandler) {
			if (eventHandler == null) {
				return;
			}

			if ((eventHandler as OnLogMessageReceived) != null) {
				LogMessageReceived += eventHandler as OnLogMessageReceived;
			}
			else if ((eventHandler as OnWarningMessageReceived) != null) {
				OnWarningReceived += eventHandler as OnWarningMessageReceived;
			}
			else if ((eventHandler as OnErrorMessageReceived) != null) {
				OnErrorReceived += eventHandler as OnErrorMessageReceived;
			}
			else {
				if (((eventHandler as OnExceptionMessageRecevied) != null)) {
					OnExceptionReceived += eventHandler as OnExceptionMessageRecevied;
				}
			}
		}

		public static void LogInfo(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LogLevels.Info, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}

		public static void LogTrace(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LogLevels.Trace, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}

		public static void LogDebug(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LogLevels.Debug, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}

		public static void LogException(this Exception e, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (e != null) {
				logger.Log(e, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}

		public static void LogWarning(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LogLevels.Warn, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}
	}
}
