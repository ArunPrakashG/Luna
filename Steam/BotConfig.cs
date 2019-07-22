using Assistant.AssistantCore;
using Assistant.Modules.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steam {
	public class BotConfig : ISteamBotConfig {
		[JsonProperty] public string SteamID { get; set; }

		[JsonProperty] public string SteamPass { get; set; }

		[JsonProperty(Required = Required.DisallowNull)] public bool Enabled { get; set; } = true;

		[JsonProperty(Required = Required.DisallowNull)] public bool SteamChatLogger { get; set; } = true;

		[JsonProperty(Required = Required.DisallowNull)] public bool RemoveSpammers { get; set; } = false;

		[JsonProperty(Required = Required.DisallowNull)] public bool AcceptFriends { get; set; } = true;

		[JsonProperty(Required = Required.DisallowNull)] public bool DeclineGroupInvites { get; set; } = false;

		[JsonProperty] public List<string> ReplyOnAdd { get; set; }

		[JsonProperty] public List<string> ChatResponses { get; set; }

		[JsonProperty] public List<string> CustomText { get; set; }

		[JsonProperty] public HashSet<uint> GamesToPlay { get; set; } = new HashSet<uint>();

		[JsonProperty] public string SteamParentalPin { get; set; } = "0";

		[JsonProperty] public bool OfflineConnection { get; set; } = false;

		[JsonProperty] public string LoginKey { get; set; } = null;

		[JsonProperty(Required = Required.DisallowNull)] public Dictionary<ulong, Enums.SteamPermissionLevels> PermissionLevel { get; set; }
	}
}
