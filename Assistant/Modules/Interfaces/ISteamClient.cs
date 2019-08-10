
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

using Assistant.Modules.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assistant.AssistantCore;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Modules.Interfaces {

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
		(bool, ConcurrentDictionary<string, ISteamBot>) InitSteamBots();

		/// <summary>
		/// Loads the steam bot configuration
		/// </summary>
		/// <returns></returns>
		List<ISteamBotConfig> LoadSteamBotConfig();

		/// <summary>
		/// The steam cell ID
		/// </summary>
		/// <returns></returns>
		uint SteamCellID { get; set; }

		/// <summary>
		/// The steam base config
		/// </summary>
		/// <returns></returns>
		ISteamConfig SteamConfig { get; set; }

		/// <summary>
		/// Saves the steam configuration
		/// </summary>
		/// <returns>Boolean value indicating status of the save</returns>
		bool SaveSteamConfig(ISteamConfig updatedConfig);

		/// <summary>
		/// Loads the steam configuration
		/// </summary>
		/// <returns>The steam configuration</returns>
		ISteamConfig LoadSteamConfig();

		/// <summary>
		/// The steam bot configuration file path
		/// </summary>
		/// <returns></returns>
		string BotConfigDirectory { get; set; }

		/// <summary>
		/// The steam configuration file path
		/// </summary>
		/// <returns></returns>
		string SteamConfigPath { get; set; }
		/// <summary>
		/// Saves the specified bot config by overwritting to the config file in steam bot directory
		/// </summary>
		/// <param name="botName">The bot name</param>
		/// <param name="updatedConfig">The config to save</param>
		/// <returns></returns>
		bool SaveSteamBotConfig(string botName, ISteamBotConfig updatedConfig);

		/// <summary>
		/// Adds the given ISteamBot instance to bot collection
		/// </summary>
		/// <param name="botName">The name of the bot to add</param>
		/// <param name="bot">The bot instance</param>
		void AddSteamBotToCollection(string botName, ISteamBot bot);

		/// <summary>
		/// Removes the bot with the specified botname from collection
		/// </summary>
		/// <param name="botName">The name of the bot to remove</param>
		void RemoveSteamBotFromCollection(string botName);

		/// <summary>
		/// Shuts down all the bots in the collection
		/// </summary>
		/// <returns></returns>
		bool DisposeAllSteamBots();

		/// <summary>
		/// Gets the user console input for the specified input context
		/// </summary>
		/// <param name="userInputType">User input context</param>
		/// <returns></returns>
		string GetUserInput(Enums.SteamUserInputType userInputType);
	}
}
