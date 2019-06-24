using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Modules.Interfaces {
	public interface IEmailClient : IModuleBase {
		ConcurrentDictionary<string, IEmailBot> EmailClientCollection { get; set; }

		(bool, ConcurrentDictionary<string, IEmailBot>) InitEmailBots ();

		void DisposeEmailBot (string botUniqueId);

		bool DisposeAllEmailBots ();

		void AddBotToCollection (string uniqueId, IEmailBot bot);

		void RemoveBotFromCollection (string uniqueId);
	}
}
