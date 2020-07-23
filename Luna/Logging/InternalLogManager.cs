using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;

namespace Luna.Logging {
	internal static class InternalLogManager {
		private static bool IsLogConfigFileExist => File.Exists(NLogConfigPath);
		private const string NLogConfigPath = "NLog.config";
		private static readonly LoggingConfiguration LoggingConfiguration;
		private static readonly LogFactory LogFactory;
		private const string GeneralTraceLayout = @"${date:format=dd-M-yyyy h\:mm} ][ ${level:uppercase=true} ][ ${logger} ][ ${message}${onexception:inner= ${exception:format=toString,Data}}";
		private const string GeneralDebugLayout = @"${date:format=dd-M-yyyy h\:mm} ][ ${logger} ][ ${message}${onexception:inner= ${exception:format=toString,Data}}";

		static InternalLogManager() {
			if (IsLogConfigFileExist) {
				LoggingConfiguration = new XmlLoggingConfiguration(NLogConfigPath);
				LogFactory = new LogFactory(LoggingConfiguration);
				return;
			}

			LoggingConfiguration config = new LoggingConfiguration();
			FileTarget TraceLogPath = new FileTarget() { FileName = Constants.TraceLogPath, Layout = GeneralTraceLayout };
			FileTarget DebugLogPath = new FileTarget() { FileName = Constants.DebugLogPath, Layout = GeneralDebugLayout };
			ColoredConsoleTarget coloredConsoleTarget = new ColoredConsoleTarget("ColoredConsole") { Layout = GeneralDebugLayout };
			config.AddTarget(TraceLogPath);
			config.AddTarget(DebugLogPath);
			config.AddTarget(coloredConsoleTarget);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, TraceLogPath));
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, DebugLogPath));
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, coloredConsoleTarget));
			LoggingConfiguration = config;
			LogFactory = new LogFactory(LoggingConfiguration);
		}

		internal static Logger GetOrCreateLogger(string loggerName) {
			if (string.IsNullOrEmpty(loggerName)) {
				throw new ArgumentNullException(nameof(loggerName));
			}

			return LogFactory.GetLogger(loggerName);
		}

		internal static Logger GetOrCreateLoggerForType<T>(T loggerType, string loggerName) {
			if (string.IsNullOrEmpty(loggerName)) {
				throw new ArgumentNullException(nameof(loggerName));
			}

			return LogFactory.GetLogger(loggerName, typeof(T));
		}

		public static void LoggerOnShutdown() {
			LogFactory.Flush();
			LogFactory.Dispose();
		}
	}
}
