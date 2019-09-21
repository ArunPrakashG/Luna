
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

using MailKit.Net.Imap;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assistant.Log;

namespace Assistant.Modules.Interfaces.EmailClientInterfaces {

	public interface IEmailBot {

		/// <summary>
		/// The gmail id of the bot (login id)
		/// </summary>
		/// <value></value>
		string GmailId { get; set; }

		/// <summary>
		/// Status if account signed in authenticated or not
		/// </summary>
		/// <value></value>
		bool IsAccountLoaded { get; set; }

		/// <summary>
		/// Collection of messages arrived during the IMAP IDLE process (if its enabled for the bot in config)
		/// </summary>
		/// <value></value>
		List<IReceviedMessageDuringIdle> MessagesArrivedDuringIdle { get; set; }

		/// <summary>
		/// The bot startup method, used to init the bot
		/// </summary>
		/// <param name="botLogger">
		/// The logger to use for this bot instance
		/// </param>
		/// <param name="mailHandler">
		/// The email client handler which was used to init the bot instance
		/// </param>
		/// <param name="botConfig">
		/// The mail bot config
		/// </param>
		/// <param name="coreClient">
		/// The main IMAP client
		/// </param>
		/// <param name="helperClient">
		/// The helper IMAP client
		/// </param>
		/// <param name="botUniqueId">
		/// The unique bot id
		/// </param>
		/// <returns>
		/// A boolean value indicating status of the bot startup
		/// IEmailBot instance of the registered bot
		/// </returns>
		Task<(bool, IEmailBot)> RegisterBot
		(
			Logger botLogger,
			IEmailClient mailHandler,
			IEmailConfig botConfig,
			ImapClient coreClient,
			ImapClient helperClient,
			string botUniqueId
		);

		/// <summary>
		/// Stop the bot IMAP IDLE service (stops push notification for the instance)
		/// </summary>
		void StopImapIdle();

		/// <summary>
		/// Disposes the bot instance.
		/// </summary>
		/// <param name="force">
		/// Triggers permenant shutdown procedure (used when exiting or to remove the bot completely)
		/// </param>
		void Dispose(bool force);
	}
}
