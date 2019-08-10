
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

using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using Assistant.Extensions;

namespace Assistant.Log {

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
