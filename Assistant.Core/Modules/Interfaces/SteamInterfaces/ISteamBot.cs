
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

using SteamKit2;
using System.Threading.Tasks;
using Assistant.Log;

namespace Assistant.Modules.Interfaces.SteamInterfaces {

	public interface ISteamBot {

		/// <summary>
		/// Indicates status if the bot is running or not
		/// </summary>
		/// <value></value>
		bool IsBotRunning { get; set; }

		/// <summary>
		/// The bot name
		/// </summary>
		/// <value></value>
		string BotName { get; set; }

		/// <summary>
		/// The bot steam 64 id
		/// </summary>
		/// <value></value>
		ulong CachedSteamId { get; set; }

		/// <summary>
		/// Loads the bot config file and required data and initiates connection with steam servers
		/// </summary>
		/// <param name="botName">The bot name</param>
		/// <param name="logger">The bot logger instance to use</param>
		/// <param name="steamClient">The bot steam client instance</param>
		/// <param name="steamHandler">The bot steam handler service</param>
		/// <param name="callbackManager">The bot steam connection callback manager</param>
		/// <param name="botConfig">The bot config</param>
		/// <returns>Boolean status and ISteamBot instance of the bot</returns>
		Task<(bool, ISteamBot)> RegisterSteamBot
		(
			string botName,
			Logger logger,
			SteamClient steamClient,
			ISteamClient steamHandler,
			CallbackManager callbackManager,
			ISteamBotConfig botConfig
		);

		/// <summary>
		/// Stops current bot instance
		/// </summary>
		void Stop();

		/// <summary>
		/// Disposes the current bot instance
		/// </summary>
		void Dispose();
	}
}
