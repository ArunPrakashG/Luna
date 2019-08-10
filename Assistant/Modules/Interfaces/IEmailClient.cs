
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

using System.Collections.Concurrent;

namespace Assistant.Modules.Interfaces {

	public interface IEmailClient : IModuleBase {

		/// <summary>
		/// The IEmailBot collection
		/// </summary>
		/// <value>String value represents the bot unique id, IEmailBot reporesents the bot instance</value>
		ConcurrentDictionary<string, IEmailBot> EmailClientCollection { get; set; }

		/// <summary>
		/// Invokes RegisterBot() method on all bots
		/// </summary>
		/// <returns>A boolean value indicating the startup status, A Dictionary with the unique id and IEmailBot instance of the specified index</returns>
		(bool, ConcurrentDictionary<string, IEmailBot>) InitEmailBots();

		/// <summary>
		/// Dispose the bot with the specified unique id
		/// </summary>
		/// <param name="botUniqueId">The unique id of the bot to remove</param>
		void DisposeEmailBot(string botUniqueId);

		/// <summary>
		/// Dispose all the IEmailBot instances which is currently running
		/// </summary>
		/// <returns>Boolean value indication status of the Dispose</returns>
		bool DisposeAllEmailBots();

		/// <summary>
		/// Adds the given instance of IEmailBot into the EmailClientCollection dictionary
		/// </summary>
		/// <param name="uniqueId">The unique id of the bot to add</param>
		/// <param name="bot">The bot instance</param>
		void AddBotToCollection(string uniqueId, IEmailBot bot);

		/// <summary>
		/// Removes the specified IEmailBot instance from EmailClientCollection dictionary using its unique id
		/// </summary>
		/// <param name="uniqueId">The unique id of the bot to remove</param>
		void RemoveBotFromCollection(string uniqueId);
	}
}
