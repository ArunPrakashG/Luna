using Assistant.Logging.Interfaces;
using System;
using System.Runtime.CompilerServices;
using static Assistant.Logging.Enums;

namespace Assistant.Logging {
	public static class LoggerExtensions {
		public static void LogInfo(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LEVEL.INFO, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}

		public static void LogTrace(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LEVEL.TRACE, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}

		public static void LogDebug(this string msg, ILogger logger,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (!string.IsNullOrEmpty(msg)) {
				logger.Log(msg, LEVEL.DEBUG, previousMethodName, callermemberlineNo, calledFilePath);
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
				logger.Log(msg, LEVEL.WARN, previousMethodName, callermemberlineNo, calledFilePath);
			}
		}
	}
}
