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

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LEVEL.DEBUG, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Error(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LEVEL.ERROR, previousMethodName, callermemberlineNo, calledFilePath));
			OnErrorReceived?.Invoke(this, new EventArgsBase(DateTime.Now, message, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Exception(Exception? exception,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {

			if (exception == null || exception.GetBaseException() == null) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(exception.ToString(), DateTime.Now, LEVEL.EXCEPTION, previousMethodName, callermemberlineNo, calledFilePath));
			OnExceptionReceived?.Invoke(this, new OnExceptionMessageEventArgs(exception, DateTime.Now, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Info(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {

			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LEVEL.INFO, previousMethodName, callermemberlineNo, calledFilePath));
		}

		public void Trace(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LEVEL.TRACE, previousMethodName, callermemberlineNo, calledFilePath));			
		}

		public void Warning(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			LogMessageReceived?.Invoke(this, new LogMessageEventArgs(message, DateTime.Now, LEVEL.WARN, previousMethodName, callermemberlineNo, calledFilePath));
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

		public void Log(string? message, LEVEL level = LEVEL.INFO,
			[CallerMemberName] string? methodName = null,
			[CallerLineNumber] int lineNo = 0,
			[CallerFilePath] string? filePath = null) {
			switch (level) {
				case LEVEL.TRACE:
					Trace($"[{Helpers.GetFileName(filePath)} | {lineNo}] {message}", methodName);
					break;

				case LEVEL.DEBUG:
					Debug(message, methodName);
					break;

				case LEVEL.INFO:
					Info(message, methodName);
					break;

				case LEVEL.WARN:
					Warning($"[{Helpers.GetFileName(filePath)} | {lineNo}] " + message, methodName);
					break;

				case LEVEL.GREEN:
					WithColor(message, ConsoleColor.Green, methodName, lineNo, filePath);
					break;

				case LEVEL.INPUT:
					Input(message, methodName, lineNo, filePath);
					break;

				case LEVEL.CYAN:
					WithColor(message, ConsoleColor.Cyan, methodName, lineNo, filePath);
					break;

				case LEVEL.CUSTOM:
					Console.WriteLine(message);
					Trace(message, methodName, lineNo, filePath);
					break;

				case LEVEL.MAGENTA:
					WithColor(message, ConsoleColor.Magenta, methodName, lineNo, filePath);
					break;

				case LEVEL.ERROR:
					Error(message, methodName, lineNo, filePath);
					break;

				case LEVEL.RED:
					WithColor(message, ConsoleColor.Red, methodName, lineNo, filePath);
					break;

				case LEVEL.BLUE:
					WithColor(message, ConsoleColor.Blue, methodName, lineNo, filePath);
					break;

				case LEVEL.EXCEPTION:
					WithColor(message, ConsoleColor.DarkRed, methodName, lineNo, filePath);
					break;

				case LEVEL.FATAL:
					WithColor(message, ConsoleColor.DarkYellow, methodName, lineNo, filePath);
					break;

				default:
					goto case LEVEL.INFO;
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
