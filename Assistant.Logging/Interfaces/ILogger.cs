using System;
using System.Runtime.CompilerServices;
using static Assistant.Logging.Enums;

namespace Assistant.Logging.Interfaces {
	public interface ILogger {

		string? LogIdentifier { get; set; }

		void InitLogger(string? logId);

		void ShutdownLogger();

		void Log(string? message, LEVEL level = LEVEL.INFO,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null);

		void Log(Exception? e,
			[CallerMemberName] string? previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string? calledFilePath = null);
	}
}
