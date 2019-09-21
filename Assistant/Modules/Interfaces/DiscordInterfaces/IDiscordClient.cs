using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assistant.Modules.Interfaces.DiscordInterfaces {
	public interface IDiscordClient : IModuleBase {
		/// <summary>
		/// Status if the bot is online or offline
		/// </summary>
		/// <value></value>
		bool IsServerOnline { get; set; }

		/// <summary>
		/// Stop discord client
		/// </summary>
		/// <returns></returns>
		Task<bool> StopDiscordService();

		/// <summary>
		/// Start discord client
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
