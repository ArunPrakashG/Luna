using System.Collections.Generic;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules.Interfaces {
	public interface ISteamBotConfig {
		string SteamID { get; set; }
		string SteamPass { get; set; }
		bool Enabled { get; set; }
		bool SteamChatLogger { get; set; }
		bool RemoveSpammers { get; set; }
		bool AcceptFriends { get; set; }
		bool DeclineGroupInvites { get; set; }
		List<string> ReplyOnAdd { get; set; }
		List<string> ChatResponses { get; set; }
		List<string> CustomText { get; set; }
		HashSet<uint> GamesToPlay { get; set; }
		string SteamParentalPin { get; set; }
		bool OfflineConnection { get; set; }
		Dictionary<ulong, SteamPermissionLevels> PermissionLevel { get; set; }
	}
}
