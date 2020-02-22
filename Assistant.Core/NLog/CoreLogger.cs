using Assistant.Extensions;
using System;
using System.Runtime.CompilerServices;
using static Assistant.Logging.Enums;

namespace Assistant.Core.NLog {
	public class CoreLogger {
		private global::NLog.Logger? LogModule;
		public string? LogIdentifier { get; private set; }

		public CoreLogger(string? loggerIdentifier) => RegisterLogger(loggerIdentifier);

		private void RegisterLogger(string? logId) {
			if (string.IsNullOrEmpty(logId)) {
				throw new ArgumentNullException(nameof(logId));
			}

			LogIdentifier = logId;
			LogModule = NLog.RegisterLogger(logId);
		}

		private void Debug(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {

			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Debug($"{previousMethodName}() {message}");
		}

		private void Error(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Error($"{previousMethodName}() {message}");
			PushbulletLog($"{previousMethodName}() {message}");
		}

		private void Exception(Exception? exception,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (exception == null) {
				return;
			}

			LogModule?.Error($"{previousMethodName}() {exception.GetBaseException().Message}/{exception.GetBaseException().HResult}/{exception.GetBaseException().StackTrace}");
			PushbulletLog($"{previousMethodName}() {exception.GetBaseException().Message}/{exception.GetBaseException().HResult}/{exception.GetBaseException().StackTrace}");
		}

		private void Info(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Info($"{message}");
		}

		private void Trace(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Trace($"{previousMethodName}() {message}");
		}

		private void Warn(string? message,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(message)) {
				NullError(nameof(message));
				return;
			}

			LogModule?.Warn($"{previousMethodName}() {message}");
			PushbulletLog($"{previousMethodName}() {message}");
			DiscordLogToChannel($"{message}");
		}

		private void NullError(string? nullObjectName,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (string.IsNullOrEmpty(nullObjectName)) {
				return;
			}

			Error($"{nullObjectName} | Object is null!", previousMethodName);
		}

		public void Log(Exception? e, LogLevels level = LogLevels.Exception,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null) {
			if (e == null) {
				return;
			}

			switch (level) {
				case LogLevels.Exception:
					Error($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.TargetSite}", previousMethodName);
					DiscordLogToChannel($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.StackTrace}");
					break;
				case LogLevels.Fatal:
					Exception(e, previousMethodName);
					break;
			}
		}

		public void WithColor(string? msg, ConsoleColor color = ConsoleColor.Green,
			[CallerMemberName] string? p = null,
			[CallerLineNumber] int c = 0,
			[CallerFilePath] string? q = null) {
			if (string.IsNullOrEmpty(msg)) {
				NullError(msg, p, c, q);
				return;
			}

			Console.ForegroundColor = color;
			Console.WriteLine(msg);
			Console.ResetColor();
			Trace(msg, p, c, q);
		}


		public void Log(string? message, LogLevels level = LogLevels.Info,
			[CallerMemberName] string? p = null,
			[CallerLineNumber] int c = 0,
			[CallerFilePath] string? q = null) {
			switch (level) {
				case LogLevels.Trace:
					Trace($"[{Helpers.GetFileName(q)} | {c}] {message}", p, c, q);
					break;
				case LogLevels.Debug:
					Debug(message, p, c, q);
					break;
				case LogLevels.Info:
					Info(message, p, c, q);
					break;
				case LogLevels.Warn:
					Warn(message, p, c, q);
					//Warn($"[{Helpers.GetFileName(q)} | {c}] " + message, p, c, q);
					break;
				case LogLevels.Custom:
					Console.WriteLine(message);
					Trace(message, p, c, q);
					break;
				case LogLevels.Error:
					Error(message, p, c, q);
					break;
				case LogLevels.Fatal:
					Error(message, p, c, q);
					break;
				case LogLevels.Green:
					WithColor(message, ConsoleColor.Green, p, c, q);
					break;
				case LogLevels.Red:
					WithColor(message, ConsoleColor.Red, p, c, q);
					break;
				case LogLevels.Blue:
					WithColor(message, ConsoleColor.Blue, p, c, q);
					break;
				case LogLevels.Cyan:
					WithColor(message, ConsoleColor.Cyan, p, c, q);
					break;
				case LogLevels.Magenta:
					WithColor(message, ConsoleColor.Magenta, p, c, q);
					break;
				case LogLevels.Input:
					Info("-> " + message, p, c, q);
					break;
				default:
					goto case LogLevels.Info;
			}
		}

		public void PushbulletLog(string? message) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			//TODO: Pushbullet logging
		}

		public void DiscordLogToChannel(string message) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			if (!Core.CoreInitiationCompleted || !Core.IsNetworkAvailable) {
				return;
			}

			Log("Logging to discord is currently turned off. [WIP]", LogLevels.Info);

			//if (Core.ModuleLoader != null && Core.ModuleLoader.Modules != null && Core.ModuleLoader.Modules.OfType<IDiscordClient>().Count() > 0) {
			//	foreach (IDiscordClient bot in Core.ModuleLoader.Modules.OfType<IDiscordClient>()) {
			//		if (bot.IsServerOnline && bot.BotConfig.EnableDiscordBot &&
			//			bot.Module.BotConfig.DiscordLogChannelID != 0 && bot.Module.BotConfig.DiscordLog) {
			//			Helpers.InBackgroundThread(async () => {
			//				await bot.Module.LogToChannel(message).ConfigureAwait(false);
			//			});
			//		}
			//	}
			//}
		}
	}
}
