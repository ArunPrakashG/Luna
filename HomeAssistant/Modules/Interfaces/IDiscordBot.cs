using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HomeAssistant.Core;

namespace HomeAssistant.Modules.Interfaces {
	public interface IDiscordBot : IModuleBase, IDiscordLogger {
		DiscordSocketClient Client { get; set; }
		CoreConfig Config { get; set; }
		bool IsServerOnline { get; set; }
		Task<bool> StopServer ();
		Task<(bool, IDiscordBot)> RegisterDiscordClient ();

		Task DiscordCoreLogger (LogMessage message);
		Task RestartDiscordServer ();

	}
}
