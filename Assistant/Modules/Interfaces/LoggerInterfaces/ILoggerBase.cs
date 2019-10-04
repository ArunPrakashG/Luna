using Assistant.AssistantCore;
using System;
using System.Runtime.CompilerServices;

namespace Assistant.Modules.Interfaces.LoggerInterfaces {

	public interface ILoggerBase {

		///<summary>
		/// Logger name.
		///</summary>
		string LogIdentifier { get; set; }

		///<summary>
		/// Identifier for the Module.
		///</summary>
		string ModuleIdentifier { get; }

		///<summary>
		/// Author for the Module.
		///</summary>
		string ModuleAuthor { get; }

		///<summary>
		/// Version for the Module.
		///</summary>
		Version ModuleVersion { get; }

		///<summary>
		/// Exception logging method.
		///</summary>
		/// <param name="e">The Exception.</param>
		/// <param name="level">The level of the log.</param>
		/// <param name="previousMethodName">The method which was invoked and produced the error.</param>
		/// <param name="callermemberlineNo">The line from which the method was invoked.</param>
		/// <param name="calledFilePath">The .cs file from which the method was invoked.</param>
		void Log
		(
			Exception e,
			Enums.LogLevels level = Enums.LogLevels.Error,
			[CallerMemberName] string previousMethodName = "",
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string calledFilePath = ""
		);

		///<summary>
		/// General logging method.
		///</summary>
		/// <param name="message">The log message.</param>
		/// <param name="level">The level of the log.</param>
		/// <param name="previousMethodName">The method which was invoked and produced the error.</param>
		/// <param name="callermemberlineNo">The line from which the method was invoked.</param>
		/// <param name="calledFilePath">The .cs file from which the method was invoked.</param>
		void Log
		(
			string message,
			Enums.LogLevels level = Enums.LogLevels.Info,
			[CallerMemberName] string previousMethodName = "",
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string calledFilePath = ""
		);

		///<summary>
		/// Discord channel logging method.
		///</summary>
		/// <param name="message">The log message to be send to the channel.</param>
		void DiscordLogToChannel(string message);

		///<summary>
		/// Logger startup method.
		///</summary>
		/// <param name="logId">Logger name</param>
		void InitLogger(string logId);

		/// <summary>
		/// Disposes the logger
		/// </summary>
		void ShutdownLogger();
	}
}
