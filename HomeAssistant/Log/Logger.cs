<<<<<<< Updated upstream:HomeAssistant/Log/Logger.cs
using Discord.WebSocket;
=======
using HomeAssistant.AssistantCore;
>>>>>>> Stashed changes:Assistant/Log/Logger.cs
using HomeAssistant.Extensions;
using HomeAssistant.Modules;
using System;
using System.Runtime.CompilerServices;
using static HomeAssistant.AssistantCore.Enums;

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

<<<<<<< Updated upstream:HomeAssistant/Log/Logger.cs
			LogModule.Trace($"{previousMethodName}() {message}");
=======
			if (AssistantCore.Core.Config.Debug) {
				LogGenericInfo($"{previousMethodName}() " + message, previousMethodName);
			}
			else {
				LogModule.Trace($"{previousMethodName}() {message}");
			}
>>>>>>> Stashed changes:Assistant/Log/Logger.cs
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

<<<<<<< Updated upstream:HomeAssistant/Log/Logger.cs
		public void Log(Exception e, ExceptionLogLevels level = ExceptionLogLevels.Error, [CallerFilePath] string filePath = null, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int previosmethodLineNumber = 0) {
			if(string.IsNullOrEmpty(e.ToString()) || string.IsNullOrWhiteSpace(e.ToString())) {
				return;
			}
=======
		public void Log(Exception e, LogLevels level = LogLevels.Error, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			switch (level) {
				case LogLevels.Error:
					if (AssistantCore.Core.Config.Debug) {
						LogGenericError($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.StackTrace}", previousMethodName);
					}
					else {
						LogGenericError($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.TargetSite}", previousMethodName);
					}
>>>>>>> Stashed changes:Assistant/Log/Logger.cs

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

		public void DiscordLogToChannel(string message) {
			if (!Program.CoreInitiationCompleted || !Program.Config.DiscordLog || !Program.Modules.Discord.IsServerOnline) {
				return;
			}

<<<<<<< Updated upstream:HomeAssistant/Log/Logger.cs
			if (DiscordClient == null && Program.Modules.Discord.Client != null) {
				DiscordClient = Program.Modules.Discord.Client;
			}
			else {
				Log("Failed to log to discord channel as the discord client is neither connected nor initialized.", LogLevels.Info);
				return;
			}

			DiscordLogger Logger = new DiscordLogger(DiscordClient, LogIdentifier);
			Helpers.InBackground(async () => await Logger.LogToChannel(message).ConfigureAwait(false));
=======
			if (!AssistantCore.Core.CoreInitiationCompleted || !AssistantCore.Core.IsNetworkAvailable) {
				return;
			}

			if (AssistantCore.Core.ModuleLoader != null && AssistantCore.Core.ModuleLoader.LoadedModules != null && AssistantCore.Core.ModuleLoader.LoadedModules.DiscordBots.Count > 0) {
				foreach ((long, IDiscordBot) bot in AssistantCore.Core.ModuleLoader.LoadedModules.DiscordBots) {
					if (bot.Item2.IsServerOnline && bot.Item2.BotConfig.EnableDiscordBot &&
					    bot.Item2.BotConfig.DiscordLogChannelID != 0 && bot.Item2.BotConfig.DiscordLog) {
						Helpers.InBackground(async () => {
							await bot.Item2.LogToChannel(message).ConfigureAwait(false);
						});
					}
				}
				
			}
>>>>>>> Stashed changes:Assistant/Log/Logger.cs
		}
	}
}
