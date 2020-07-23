using Luna.Extensions;
using NLog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Luna.Logging {
	internal class InternalLogger {
		private readonly string Identifier;
		private readonly Logger LogModule;

		internal InternalLogger(string loggerIdentifier) {
			Identifier = loggerIdentifier ?? throw new ArgumentNullException(nameof(loggerIdentifier));
			LogModule = InternalLogManager.GetOrCreateLogger(Identifier);
		}

		internal InternalLogger() {
			MethodBase? methodInfo = new StackTrace().GetFrame(1)?.GetMethod();
			string? className = methodInfo?.ReflectedType?.Name;
			Identifier = !string.IsNullOrEmpty(className) ? className : "Unspecified";
			LogModule = InternalLogManager.GetOrCreateLogger(Identifier);
		}

		internal InternalLogger(Logger loggerModule) {
			MethodBase? methodInfo = new StackTrace().GetFrame(1)?.GetMethod();
			string? className = methodInfo?.ReflectedType?.Name;
			Identifier = !string.IsNullOrEmpty(className) ? className : "Unspecified";
			LogModule = loggerModule ?? throw new ArgumentNullException(nameof(loggerModule));
		}

		internal InternalLogger(Logger loggerModule, string loggerIdentifier) {
			Identifier = loggerIdentifier ?? throw new ArgumentNullException(nameof(loggerIdentifier));
			LogModule = loggerModule ?? throw new ArgumentNullException(nameof(loggerModule));
		}

		internal static InternalLogger GetOrCreateLogger<T>(T type, string identifier) {
			if (string.IsNullOrEmpty(identifier)) {
				throw new ArgumentNullException(nameof(identifier));
			}

			return new InternalLogger(InternalLogManager.GetOrCreateLoggerForType<T>(type, identifier), identifier);
		}

		internal void Debug(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Debug($"{calledFilePath} -> {callermemberlineNo} | {previousMethodName}() {message}");
		}

		internal void Trace(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Trace($"{calledFilePath} -> {callermemberlineNo} | {previousMethodName}() {message}");
		}

		internal void Info(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Info($"{message}");
		}

		internal void Error(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Error($"{previousMethodName}() {message}");
		}

		internal void Fatal(Exception? exception,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (exception == null) {
				NullError(nameof(exception));
				return;
			}

			LogModule.Fatal(exception);
		}

		internal void Exception(Exception? exception,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (exception == null) {
				NullError(nameof(exception));
				return;
			}

			LogModule?.Error($"{previousMethodName}() {exception.GetBaseException().Message}/{exception.GetBaseException().HResult}/{exception.GetBaseException().StackTrace}");			
		}		

		internal void Warn(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Warn($"{previousMethodName}() {message}");
		}

		internal void NullError(string? nullObjectName,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(nullObjectName)) {
				return;
			}

			Error($"{nullObjectName} | Object is null!", previousMethodName);
		}

		internal void Push(string? message) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			//TODO: Pushbullet logging
		}

		internal void InputLog(string inputText, ConsoleColor foregroundColor = ConsoleColor.White) {
			if (string.IsNullOrEmpty(inputText)) {
				NullError(nameof(inputText));
				return;
			}

			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(">>> " + inputText);
			Console.ResetColor();
		}

		internal void CustomLog(string message, ConsoleColor foregroundColor = ConsoleColor.White) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(message);
			Console.ResetColor();
		}
	}
}
