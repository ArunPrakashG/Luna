
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Modules.Interfaces;
using Assistant.PushBullet;
using Assistant.PushBullet.Parameters;
using System;
using System.Runtime.CompilerServices;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Log {

	public class Logger : ILoggerBase {
		private NLog.Logger LogModule;

		public string LogIdentifier { get; set; }

		public string ModuleIdentifier => nameof(Logger);

		public string ModuleAuthor => "Arun Prakash";

		public Version ModuleVersion => new Version("6.0.0.0");

		public Logger(string loggerIdentifier) => RegisterLogger(loggerIdentifier);

		private void RegisterLogger(string logId) {
			if (string.IsNullOrEmpty(logId)) {
				throw new ArgumentNullException(nameof(logId));
			}

			LogModule = Logging.RegisterLogger(logId);
			LogIdentifier = logId;
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

			if (Core.Config.PushBulletLogging && Core.PushBulletService != null && Core.PushBulletService.IsBroadcastServiceOnline) {
				Core.PushBulletService.BroadcastMessage(new PushRequestContent() {
					PushTarget = PushEnums.PushTarget.All,
					PushTitle = $"{Core.AssistantName} [ERROR] LOG",
					PushType = PushEnums.PushType.Note,
					PushBody = $"{previousMethodName}() {message}"
				});
			}
		}

		private void LogGenericException(Exception exception, [CallerMemberName] string previousMethodName = null) {
			if (exception == null) {
				LogNullError(nameof(exception));
				return;
			}

			LogModule.Error($"{previousMethodName}() {exception.GetBaseException().Message}/{exception.GetBaseException().HResult}/{exception.GetBaseException().StackTrace}");

			if (Core.Config.PushBulletLogging && Core.PushBulletService != null && Core.PushBulletService.IsBroadcastServiceOnline) {
				Core.PushBulletService.BroadcastMessage(new PushRequestContent() {
					PushTarget = PushEnums.PushTarget.All,
					PushTitle = $"{Core.AssistantName} [EXCEPTION] LOG",
					PushType = PushEnums.PushType.Note,
					PushBody = $"{previousMethodName}() {exception.GetBaseException().Message}/{exception.GetBaseException().HResult}/{exception.GetBaseException().StackTrace}"
				});
			}
		}

		private void LogGenericInfo(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			LogModule.Info($"{message}");
		}

		private void LogGenericTrace(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			if (Core.Config.Debug) {
				LogGenericInfo($"{previousMethodName}() " + message, previousMethodName);
			}
			else {
				LogModule.Trace($"{previousMethodName}() {message}");
			}
		}

		private void LogGenericWarning(string message, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(message)) {
				LogNullError(nameof(message));
				return;
			}

			LogModule.Warn($"{previousMethodName}() {message}");

			if (Core.Config.PushBulletLogging && Core.PushBulletService != null && Core.PushBulletService.IsBroadcastServiceOnline) {
				Core.PushBulletService.BroadcastMessage(new PushRequestContent() {
					PushTarget = PushEnums.PushTarget.All,
					PushTitle = $"{Core.AssistantName} [WARNING] LOG",
					PushType = PushEnums.PushType.Note,
					PushBody = $"{previousMethodName}() {message}"
				});
			}
		}

		private void LogNullError(string nullObjectName, [CallerMemberName] string previousMethodName = null) {
			if (string.IsNullOrEmpty(nullObjectName)) {
				return;
			}

			LogGenericError($"{nullObjectName} | Object is null!", previousMethodName);
		}

		public void Log(Exception e, LogLevels level = LogLevels.Error, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			switch (level) {
				case Enums.LogLevels.Error:
					if (Core.Config.Debug) {
						LogGenericError($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.StackTrace}", previousMethodName);
					}
					else {
						LogGenericError($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.TargetSite}", previousMethodName);
					}

					DiscordLogToChannel($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.StackTrace}");
					break;

				case Enums.LogLevels.Fatal:
					LogGenericException(e, previousMethodName);
					break;

				case Enums.LogLevels.DebugException:
					LogGenericError($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.StackTrace}", previousMethodName);
					DiscordLogToChannel($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + $"{e.Message} | {e.StackTrace}");
					break;

				default:
					goto case Enums.LogLevels.Error;
			}
		}

		public void Log(string message, LogLevels level = LogLevels.Info, [CallerMemberName] string previousMethodName = null, [CallerLineNumber] int callermemberlineNo = 0, [CallerFilePath] string calledFilePath = null) {
			switch (level) {
				case Enums.LogLevels.Trace:
					LogGenericTrace($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] {message}", previousMethodName);
					break;

				case Enums.LogLevels.Debug:
					LogGenericDebug(message, previousMethodName);
					break;

				case Enums.LogLevels.Info:
					LogGenericInfo(message, previousMethodName);
					break;

				case Enums.LogLevels.Warn:
					LogGenericWarning($"[{Helpers.GetFileName(calledFilePath)} | {callermemberlineNo}] " + message, previousMethodName);

					if (!string.IsNullOrEmpty(message) || !string.IsNullOrWhiteSpace(message)) {
						DiscordLogToChannel($"{message}");
					}

					break;

				case Enums.LogLevels.Ascii:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(message);
					Console.ResetColor();
					LogGenericTrace(message);
					break;

				case Enums.LogLevels.UserInput:
					Console.WriteLine(@">>> " + message);
					LogGenericTrace(message);
					break;

				case Enums.LogLevels.ServerResult:
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine(@"> " + message);
					Console.ResetColor();
					LogGenericTrace(message);
					break;

				case Enums.LogLevels.Custom:
					Console.WriteLine(message);
					LogGenericTrace(message, previousMethodName);
					break;

				case Enums.LogLevels.Sucess:
					Console.ForegroundColor = ConsoleColor.DarkMagenta;
					LogGenericInfo(message, previousMethodName);
					Console.ResetColor();
					break;

				default:
					goto case Enums.LogLevels.Info;
			}
		}

		public void DiscordLogToChannel(string message) {
			if (Helpers.IsNullOrEmpty(message)) {
				return;
			}

			if (!Core.CoreInitiationCompleted || !Core.IsNetworkAvailable) {
				return;
			}

			if (Core.ModuleLoader != null && Core.ModuleLoader.ModulesCollection != null && Core.ModuleLoader.ModulesCollection.DiscordBots.Count > 0) {
				foreach (Modules.ModuleInfo<IDiscordBot> bot in Core.ModuleLoader.ModulesCollection.DiscordBots) {
					if (bot.Module.IsServerOnline && bot.Module.BotConfig.EnableDiscordBot &&
						bot.Module.BotConfig.DiscordLogChannelID != 0 && bot.Module.BotConfig.DiscordLog) {
						Helpers.InBackgroundThread(async () => {
							await bot.Module.LogToChannel(message).ConfigureAwait(false);
						});
					}
				}
			}
		}

		public void InitLogger(string logId) => RegisterLogger(logId);

		public void ShutdownLogger() => Logging.LoggerOnShutdown();
	}
}
