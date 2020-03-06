using Assistant.Logging.EventArgs;
using Assistant.Rest;
using System;
using System.Collections.Generic;
using static Assistant.Logging.Enums;

namespace Assistant.Core {
	internal class EventManager {
		private static List<NLog.CoreLogger> AssistantLoggers = new List<NLog.CoreLogger>();

		internal void Logger_OnWarningReceived(object sender, EventArgsBase e) { }

		internal void Logger_OnInputReceived(object sender, EventArgsBase e) { }

		internal void Logger_OnExceptionReceived(object sender, OnExceptionMessageEventArgs e) {
			if (AssistantLoggers == null) {
				AssistantLoggers = new List<NLog.CoreLogger>();
			}

			NLog.CoreLogger? logger = AssistantLoggers.Find(x => !string.IsNullOrEmpty(x.LogIdentifier) && x.LogIdentifier.Equals(e.LogIdentifier, StringComparison.OrdinalIgnoreCase));

			if (logger == null) {
				logger = new NLog.CoreLogger(e.LogIdentifier);
				AssistantLoggers.Add(logger);
			}

			logger.Log(e.LogException, LogLevels.Exception, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
		}

		internal void Logger_OnErrorReceived(object sender, EventArgsBase e) {
			if (AssistantLoggers == null) {
				AssistantLoggers = new List<NLog.CoreLogger>();
			}

			NLog.CoreLogger? logger = AssistantLoggers.Find(x => !string.IsNullOrEmpty(x.LogIdentifier) && x.LogIdentifier.Equals(e.LogIdentifier, StringComparison.OrdinalIgnoreCase));

			if (logger == null) {
				logger = new NLog.CoreLogger(e.LogIdentifier);
				AssistantLoggers.Add(logger);
			}

			logger.Log(e.LogMessage, LogLevels.Error, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
		}

		internal void Logger_OnColoredReceived(object sender, WithColorEventArgs e) { }

		internal void Logger_LogMessageReceived(object sender, LogMessageEventArgs e) {
			if (AssistantLoggers == null) {
				AssistantLoggers = new List<NLog.CoreLogger>();
			}

			NLog.CoreLogger? logger = AssistantLoggers.Find(x => !string.IsNullOrEmpty(x.LogIdentifier) && x.LogIdentifier.Equals(e.LogIdentifier, StringComparison.OrdinalIgnoreCase));

			if (logger == null) {
				logger = new NLog.CoreLogger(e.LogIdentifier);
				AssistantLoggers.Add(logger);
			}

			switch (e.LogLevel) {
				case LogLevels.Trace:
					logger.Log(e.LogMessage, LogLevels.Trace, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Debug:
					logger.Log(e.LogMessage, LogLevels.Debug, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Info:
					logger.Log(e.LogMessage, LogLevels.Info, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Warn:
					logger.Log(e.LogMessage, LogLevels.Warn, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Error:
				case LogLevels.Exception:
				case LogLevels.Fatal:
					break;
				case LogLevels.Green:
					logger.Log(e.LogMessage, LogLevels.Green, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Red:
					logger.Log(e.LogMessage, LogLevels.Red, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Blue:
					logger.Log(e.LogMessage, LogLevels.Blue, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Cyan:
					logger.Log(e.LogMessage, LogLevels.Cyan, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Magenta:
					logger.Log(e.LogMessage, LogLevels.Magenta, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Input:
					logger.Log(e.LogMessage, LogLevels.Input, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
				case LogLevels.Custom:
					logger.Log(e.LogMessage, LogLevels.Custom, e.CallerMemberName, e.CallerLineNumber, e.CallerFilePath);
					break;
			}
		}

		internal RequestResponse RestServerExampleCommand(RequestParameter arg) {
			return new RequestResponse();
		}
	}
}
