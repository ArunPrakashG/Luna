using Luna.Logging;
using Luna.Logging.EventArgs;
using Luna.Logging.Interfaces;
using Luna.Rest;
using FluentScheduler;
using System;
using System.Collections.Generic;
using static Luna.Logging.Enums;

namespace Luna.Core {
	internal static class CoreEventManager {
		private static List<NLog.CoreLogger> AssistantLoggers = new List<NLog.CoreLogger>();
		private static readonly ILogger Logger = new Logger(typeof(CoreEventManager).Name);

		static CoreEventManager() {
			Logging.Logger.LogMessageReceived += OnLogMessageReceived;
			Logging.Logger.OnColoredReceived += OnColoredReceived;
			Logging.Logger.OnErrorReceived += OnErrorReceived;
			Logging.Logger.OnExceptionReceived += OnExceptionOccured;
			Logging.Logger.OnInputReceived += OnInputReceived;
			Logging.Logger.OnWarningReceived += OnWarningReceived;
			JobManager.JobException += JobManagerOnException;
			JobManager.JobStart += JobManagerOnJobStart;
			JobManager.JobEnd += JobManagerOnJobEnd;
		}

		internal static void Init() { }

		internal static void OnWarningReceived(object sender, EventArgsBase e) { }

		internal static void OnInputReceived(object sender, EventArgsBase e) { }

		internal static void OnExceptionOccured(object sender, OnExceptionMessageEventArgs e) {
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

		internal static void OnErrorReceived(object sender, EventArgsBase e) {
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

		internal static void OnColoredReceived(object sender, WithColorEventArgs e) { }

		internal static void OnLogMessageReceived(object sender, LogMessageEventArgs e) {
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

		internal static void JobManagerOnException(JobExceptionInfo obj) => Logger.Exception(obj.Exception);

		internal static void JobManagerOnJobEnd(JobEndInfo obj) {
			if(obj.Name.Equals("ConsoleUpdater", StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			Logger.Trace($"A job has ended -> {obj.Name} / {obj.StartTime.ToString()}");
		}

		internal static void JobManagerOnJobStart(JobStartInfo obj) {
			if (obj.Name.Equals("ConsoleUpdater", StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			Logger.Trace($"A job has started -> {obj.Name} / {obj.StartTime.ToString()}");
		}

		internal static RequestResponse RestServerExampleCommand(RequestParameter arg) {
			return new RequestResponse();
		}
	}
}
