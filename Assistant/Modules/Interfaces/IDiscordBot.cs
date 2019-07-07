using Discord.WebSocket;
using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {

	public interface IDiscordBot : IModuleBase, IDiscordLogger {

		/// <summary>
		/// The discord bot client
		/// </summary>
		/// <value></value>
		DiscordSocketClient Client { get; set; }

		/// <summary>
		/// The discord bot config
		/// </summary>
		/// <value></value>
		IDiscordBotConfig BotConfig { get; set; }

		/// <summary>
		/// Status if the bot is online or offline
		/// </summary>
		/// <value></value>
		bool IsServerOnline { get; set; }

		/// <summary>
		/// Stop discord client
		/// </summary>
		/// <returns></returns>
		Task<bool> StopServer();

		/// <summary>
		/// Start discord bot
		/// </summary>
		/// <returns></returns>
		Task<(bool, IDiscordBot)> RegisterDiscordClient();

		/// <summary>
		/// Restart discord bot service
		/// </summary>
		/// <returns></returns>
		Task RestartDiscordServer();
	}
}
