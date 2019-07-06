using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using AssistantCore;

namespace HomeAssistant.Modules.Interfaces {
	public interface ISteamClient : IModuleBase {
		/// <summary>
		/// The collection of initiated ISteamBot instances
		/// </summary>
		/// <value></value>
		ConcurrentDictionary<string, ISteamBot> SteamBotCollection { get; set; }
		/// <summary>
		/// Reads each bot in steam bot config directory and starts an instance of the bot
		/// </summary>
		/// <returns>Boolean value indicating status of the Initiation, Dictionary contains collection of the initiated bots</returns>
		(bool, ConcurrentDictionary<string, ISteamBot>) InitSteamBots ();
		/// <summary>
		/// Loads the steam bot configuration
		/// </summary>
		/// <returns></returns>
		List<ISteamBotConfig> LoadConfig ();
		/// <summary>
		/// Saves the specified bot config by overwritting to the config file in steam bot directory
		/// </summary>
		/// <param name="botName">The bot name</param>
		/// <param name="updatedConfig">The config to save</param>
		/// <returns></returns>
		bool SaveConfig (string botName, ISteamBotConfig updatedConfig);
		/// <summary>
		/// Adds the given ISteamBot instance to bot collection
		/// </summary>
		/// <param name="botName">The name of the bot to add</param>
		/// <param name="bot">The bot instance</param>
		void AddBotToCollection (string botName, ISteamBot bot);
		/// <summary>
		/// Removes the bot with the specified botname from collection
		/// </summary>
		/// <param name="botName">The name of the bot to remove</param>
		void RemoveBotFromCollection (string botName);
		/// <summary>
		/// Shuts down all the bots in the collection
		/// </summary>
		/// <returns></returns>
		bool DisposeAllBots ();
		/// <summary>
		/// Gets the user console input for the specified input context
		/// </summary>
		/// <param name="userInputType">User input context</param>
		/// <returns></returns>
		string GetUserInput (Enums.SteamUserInputType userInputType);

	}
}
