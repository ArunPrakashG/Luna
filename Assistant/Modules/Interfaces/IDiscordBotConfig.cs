namespace HomeAssistant.Modules.Interfaces {

	public interface IDiscordBotConfig {

		bool EnableDiscordBot { get; set; }

		ulong DiscordOwnerID { get; set; }

		ulong DiscordServerID { get; set; }

		ulong DiscordLogChannelID { get; set; }

		bool DiscordLog { get; set; }

		string DiscordBotToken { get; set; }
	}
}
