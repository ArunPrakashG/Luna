using Assistant.Logging.EventArgs;
using Assistant.Logging.Interfaces;
using System;
using System.Runtime.CompilerServices;
using static Assistant.Logging.Enums;

namespace Assistant.Logging {
	public class Logger : ILogger {
		public string? LogIdentifier { get; set; }

		public Logger(string loggerIdentifier) => RegisterLogger(loggerIdentifier);

		public delegate void OnLogMessageReceived(object sender, LogMessageEventArgs e);
		public static event OnLogMessageReceived? LogMessageReceived;

		public delegate void OnWarningMessageReceived(object sender, EventArgsBase e);
		public static event OnWarningMessageReceived? OnWarningReceived;

		public delegate void OnErrorMessageReceived(object sender, EventArgsBase e);
		public static event OnErrorMessageReceived? OnErrorReceived;

		public delegate void OnExceptionMessageRecevied(object sender, OnExceptionMessageEventArgs e);
		public static event OnExceptionMessageRecevied? OnExceptionReceived;

		public void Debug(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LogLevels.Debug, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Error(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LogLevels.Error, previousMethodName, callermemberlineNo, calledFilePath));
			OnErrorReceived?.Invoke(this, new EventArgsBase(DateTime.Now, message, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Exception(Exception? exception,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {

			if (exception == null || exception.GetBaseException() == null) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(exception.ToString(), DateTime.Now, LogLevels.Exception, previousMethodName, callermemberlineNo, calledFilePath));
			OnExceptionReceived?.Invoke(this, new OnExceptionMessageEventArgs(exception, DateTime.Now, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Info(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {

			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LogLevels.Info, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Trace(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LogLevels.Trace, previousMethodName, callermemberlineNo, calledFilePath));			
		}

		public void Warning(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LogLevels.Warn, previousMethodName, callermemberlineNo, calledFilePath));
			OnWarningReceived?.Invoke(this, new EventArgsBase(DateTime.Now, message, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void WithColor(string? message, ConsoleColor color = ConsoleColor.Cyan,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			Console.ResetColor();
			Trace(message, previousMethodName, callermemberlineNo, calledFilePath);
		}

		public void Input(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			Console.WriteLine("-> " + message);
			Trace(message, previousMethodName, callermemberlineNo, calledFilePath);
		}

		public void Log(Exception? e,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {

			if (e == null) {
				return;
			}

			Exception(e, previousMethodName, callermemberlineNo, calledFilePath);
		}

		public void Log(string? message, LogLevels level = LogLevels.Info,
			[CallerMemberName] string? methodName = null,
			[CallerLineNumber] int lineNo = 0,
			[CallerFilePath] string? filePath = null) {
			switch (level) {
				case LogLevels.Trace:
					Trace($"[{Helpers.GetFileName(filePath)} | {lineNo}] {message}", methodName);
					break;

				case LogLevels.Debug:
					Debug(message, methodName);
					break;

				case LogLevels.Info:
					Info(message, methodName);
					break;

				case LogLevels.Warn:
					Warning($"[{Helpers.GetFileName(filePath)} | {lineNo}] " + message, methodName);
					break;

				case LogLevels.Green:
					WithColor(message, ConsoleColor.Green, methodName, lineNo, filePath);
					break;

				case LogLevels.Input:
					Input(message, methodName, lineNo, filePath);
					break;

				case LogLevels.Cyan:
					WithColor(message, ConsoleColor.Cyan, methodName, lineNo, filePath);
					break;

				case LogLevels.Custom:
					Console.WriteLine(message);
					Trace(message, methodName, lineNo, filePath);
					break;

				case LogLevels.Magenta:
					WithColor(message, ConsoleColor.Magenta, methodName, lineNo, filePath);
					break;

				case LogLevels.Error:
					Error(message, methodName, lineNo, filePath);
					break;

				case LogLevels.Red:
					WithColor(message, ConsoleColor.Red, methodName, lineNo, filePath);
					break;

				case LogLevels.Blue:
					WithColor(message, ConsoleColor.Blue, methodName, lineNo, filePath);
					break;

				case LogLevels.Exception:
					WithColor(message, ConsoleColor.DarkRed, methodName, lineNo, filePath);
					break;

				case LogLevels.Fatal:
					WithColor(message, ConsoleColor.DarkYellow, methodName, lineNo, filePath);
					break;

				default:
					goto case LogLevels.Info;
			}
		}

		public void InitLogger(string? logId) => RegisterLogger(logId);

		private void RegisterLogger(string? logId) {
			if (string.IsNullOrEmpty(logId)) {
				throw new ArgumentNullException(nameof(logId));
			}

			LogIdentifier = logId;
		}

		public void ShutdownLogger() {
			
		}
	}
}
