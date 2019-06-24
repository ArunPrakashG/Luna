using HomeAssistant.Log;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {
	public interface ISteamBot {
		bool IsBotRunning { get; set; }
		string BotName { get; set; }
		ulong Steam64ID { get; set; }

		Task<(bool, ISteamBot)> RegisterSteamBot
		(
			string botName,
			Logger logger,
			SteamClient steamClient,
			ISteamClient steamHandler,
			CallbackManager callbackManager,
			ISteamBotConfig botConfig
		);

		void Stop ();

		void Dispose ();

	}
}
