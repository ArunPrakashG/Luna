using HomeAssistant.Log;
using SteamKit2;
using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {

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
