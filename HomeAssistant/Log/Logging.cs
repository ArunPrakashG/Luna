using HomeAssistant.Extensions;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;

namespace HomeAssistant.Log {

	public class Logging {
		private const string GeneralTraceLayout = @"${date:format=dd-M-yyyy h\:mm} ][ ${level:uppercase=true} ][ ${logger} ][ ${message}${onexception:inner= ${exception:format=toString,Data}}";
		private const string GeneralDebugLayout = @"${date:format=dd-M-yyyy h\:mm} ][ ${logger} ][ ${message}${onexception:inner= ${exception:format=toString,Data}}";

		public static bool IsLoggerOnline { get; set; } = true;
		public static NLog.Logger RegisterLogger(string loggerName) {
			if (string.IsNullOrEmpty(loggerName)) {
				throw new ArgumentException("message", nameof(loggerName));
			}

			if (File.Exists("NLog.config")) {
				LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
				return LogManager.GetLogger(loggerName);
			}
			else {
				LoggingConfiguration Config = new LoggingConfiguration();

				FileTarget TraceLogPath = new FileTarget() {
					FileName = Constants.TraceLogPath,
					Layout = GeneralTraceLayout
				};
				ColoredConsoleTarget coloredConsoleTarget = new ColoredConsoleTarget("ColoredConsole") { Layout = GeneralDebugLayout };

				FileTarget DebugLogPath = new FileTarget() {
					FileName = Constants.DebugLogPath,
					Layout = GeneralDebugLayout
				};

				Config.AddTarget(TraceLogPath);
				Config.AddTarget(DebugLogPath);
				Config.AddTarget(coloredConsoleTarget);

				Config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, TraceLogPath));
				Config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, DebugLogPath));
				Config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, coloredConsoleTarget));

				LogManager.Configuration = Config;
				return LogManager.GetLogger(loggerName);
			}
		}

		public static void LoggerOnShutdown() {
			IsLoggerOnline = false;
			LogManager.Flush();
			LogManager.Shutdown();
		}
	}
}
