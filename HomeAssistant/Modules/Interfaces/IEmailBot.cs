using HomeAssistant.Log;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HomeAssistant.Core;
using MailKit.Net.Imap;

namespace HomeAssistant.Modules.Interfaces {
	public interface IEmailBot {
		string GmailId { get; set; }
		bool IsAccountLoaded { get; set; }
		List<IReceviedMessageDuringIdle> MessagesArrivedDuringIdle { get; set; }

		Task<(bool, IEmailBot)> RegisterBot
		(
			Logger botLogger,
			IEmailClient mailHandler,
			IEmailConfig botConfig,
			ImapClient coreClient,
			ImapClient helperClient,
			string botUniqueId
		);

		void StopImapIdle ();

		void Dispose ();

	}
}
