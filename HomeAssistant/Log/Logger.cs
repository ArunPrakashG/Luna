using Discord.WebSocket;
using HomeAssistant.Extensions;
using HomeAssistant.Modules;
using System;
using System.Runtime.CompilerServices;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Log {

	public class Logger {
		private NLog.Logger LogModule;
		private DiscordSocketClient DiscordClient;
		private readonly string LogIdentifier;

		public Logger(string loggerIdentifier) {
			if (string.IsNullOrEmpty(loggerIdentifier)) {
				throw new ArgumentNullException(nameof(loggerIdentifier));
			}

			LogModule = Logging.RegisterLogger(loggerIdentifier);
			LogIdentifier = loggerIdentifier;
		}

		private void LogGenericDebug(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			LogModule.Debug($"{previousMethodName}() {message}");
		}

		private void LogGenericError(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			LogModule.Error($"{previousMethodName}() {message}");
		}

		private void LogGenericException(Exception exception, [CallerMemberName] string previousMethodName = null) {
			if (exception == null) {
				LogNullError(nameof(exception));
				return;
			}

			LogModule.Error($"{previousMethodName}() {exception.GetBaseException().Message}/{exception.GetBaseException().HResult}/{exception.GetBaseException().StackTrace}");
		}

		private void LogGenericInfo(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			if (Program.Config.Debug) {
				LogModule.Info($"{previousMethodName}() {message}");
			}
			else {
				LogModule.Info($"{message}");
			}
		}

		private void LogGenericTrace(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			LogModule.Trace($"{previousMethodName}() {message}");
		}

		private void LogGenericWarning(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			LogModule.Warn($"{previousMethodName}() {message}");
		}

		private void LogNullError(string nullObjectName, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(nullObjectName)) {
				return;
			}

			LogGenericError($"{nullObjectName} | Object is null!", previousMethodName);
		}

		public void Log(string message, LogLevels level = LogLevels.Info, [CallerMemberName] string previousMethodName = null) {
			switch (level) {
				case LogLevels.Trace:
					if (Program.Config.Debug) {
						LogGenericInfo(message, previousMethodName);
					}
					LogGenericTrace(message, previousMethodName);
					break;

				case LogLevels.Debug:
					LogGenericDebug(message, previousMethodName);
					break;

				case LogLevels.Info:
					LogGenericInfo(message, previousMethodName);
					break;

				case LogLevels.Warn:
					LogGenericWarning(message, previousMethodName);
					if (!string.IsNullOrEmpty(message) || !string.IsNullOrWhiteSpace(message)) {
						DiscordLogToChannel($"{message}");
					}
					break;

				case LogLevels.Error:
					LogGenericError(message, previousMethodName);
					if (!string.IsNullOrEmpty(message) || !string.IsNullOrWhiteSpace(message)) {
						DiscordLogToChannel($"{message}");
					}
					break;

				case LogLevels.Ascii:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(message);
					Console.ResetColor();
					LogGenericTrace(message);
					break;

				case LogLevels.UserInput:
					Console.WriteLine(">>> " + message);
					LogGenericTrace(message);
					break;

				case LogLevels.ServerResult:
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine("> " + message);
					Console.ResetColor();
					LogGenericTrace(message);
					break;

				case LogLevels.Custom:
					Console.WriteLine(message);
					LogGenericTrace(message, previousMethodName);
					break;

				case LogLevels.Sucess:
					Console.ForegroundColor = ConsoleColor.DarkMagenta;
					LogGenericInfo(message, previousMethodName);
					Console.ResetColor();
					break;

				default:
					goto case LogLevels.Info;
			}
		}

		public void Log(Exception e, ExceptionLogLevels level = ExceptionLogLevels.Error, [CallerFilePath] string filePath = null, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int previosmethodLineNumber = 0) {
			if (string.IsNullOrEmpty(e.ToString()) || string.IsNullOrWhiteSpace(e.ToString())) {
				return;
			}

			switch (level) {
				case ExceptionLogLevels.Fatal:
					LogGenericException(e, previousMethodName);
					break;

				case ExceptionLogLevels.Error:
					LogGenericError($"[{previosmethodLineNumber} line] {previousMethodName}() {e.Message}/{e.InnerException}/{e.StackTrace}");
					break;

				case ExceptionLogLevels.DebugException:
					LogGenericTrace($"[{previosmethodLineNumber} line] {previousMethodName}() {e.Message}/{e.InnerException}/{e.StackTrace}");
					break;

				default:
					goto case ExceptionLogLevels.Error;
			}

			if (!string.IsNullOrEmpty(e.ToString()) || !string.IsNullOrWhiteSpace(e.ToString())) {
				DiscordLogToChannel($"{e.ToString()}\n\nLine Number: {previosmethodLineNumber}\n{previousMethodName}()");
			}
		}

		public void DiscordLogToChannel(string message) {
			if (!Program.CoreInitiationCompleted || !Program.Config.DiscordLog || !Program.Modules.Discord.IsServerOnline) {
				return;
			}

			if (DiscordClient == null && Program.Modules.Discord.Client != null) {
				DiscordClient = Program.Modules.Discord.Client;
			}
			else {
				Log("Failed to log to discord channel as the discord client is neither connected nor initialized.", LogLevels.Info);
				return;
			}

			DiscordLogger Logger = new DiscordLogger(DiscordClient, LogIdentifier);
			Helpers.InBackground(async () => await Logger.LogToChannel(message).ConfigureAwait(false));
		}
	}
}
