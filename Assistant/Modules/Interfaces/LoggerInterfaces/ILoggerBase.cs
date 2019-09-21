
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
			[CallerMemberName] string previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string calledFilePath = null
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
			[CallerMemberName] string previousMethodName = null,
			[CallerLineNumber] int callermemberlineNo = 0,
			[CallerFilePath] string calledFilePath = null
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
