using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using HomeAssistant.Core;

namespace HomeAssistant.Modules.Interfaces {
	public interface ISteamClient : IModuleBase {
		ConcurrentDictionary<string, ISteamBot> SteamBotCollection { get; set; }

		(bool, ConcurrentDictionary<string, ISteamBot>) InitSteamBots ();

		List<ISteamBotConfig> LoadConfig ();

		bool SaveConfig (string botName, ISteamBotConfig updatedConfig);

		void AddBotToCollection (string botName, ISteamBot bot);

		void RemoveBotFromCollection (string botName);

		bool DisposeAllBots ();

		string GetUserInput (Enums.SteamUserInputType userInputType);

	}
}
