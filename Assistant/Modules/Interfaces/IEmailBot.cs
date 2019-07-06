using HomeAssistant.Log;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HomeAssistant.AssistantCore;
using MailKit.Net.Imap;

namespace HomeAssistant.Modules.Interfaces {
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
		void StopImapIdle ();
		/// <summary>
		/// Disposes the bot instance.
		/// </summary>
		/// <param name="force">
		/// Triggers permenant shutdown procedure (used when exiting or to remove the bot completely)
		/// </param>
		void Dispose (bool force);

	}
}
