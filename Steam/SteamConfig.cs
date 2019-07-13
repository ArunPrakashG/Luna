using Assistant.Modules.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Steam {
	public class SteamConfig : ISteamConfig {
		[JsonProperty] public ulong[] BlacklistedSteamIds { get; set; }
		[JsonProperty] public string CommandPrefix { get; set; } = "!";
		[JsonProperty] public Dictionary<ulong, byte> Owners { get; set; } = new Dictionary<ulong, byte>();
		[JsonProperty] public string OwnerBotName { get; set; } = "Nikku";
		[JsonProperty] public bool ConfigAutoBackup { get; set; } = false;
		[JsonProperty] public string[] BlacklistedWords { get; set; }
		[JsonProperty] public int ChatResponseDelay { get; set; } = 30;
		[JsonProperty] public int ConfigBackupDelay { get; set; } = 5;
		[JsonProperty] public int DisconnectSleepDelay { get; set; } = 5;
		[JsonProperty] public int MaxDisconnectsBeforeSleep { get; set; } = 5;
	}
}
