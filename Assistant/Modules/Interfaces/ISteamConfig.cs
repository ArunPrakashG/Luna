using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Modules.Interfaces {
	public interface ISteamConfig {
		ulong[] BlacklistedSteamIds { get; set; }
		string CommandPrefix { get; set; }
		Dictionary<ulong, byte> Owners { get; set; }
		string OwnerBotName { get; set; }
		bool ConfigAutoBackup { get; set; }
		string[] BlacklistedWords { get; set; }
		int ChatResponseDelay { get; set; }
		int ConfigBackupDelay { get; set; }
		int DisconnectSleepDelay { get; set; }
		int MaxDisconnectsBeforeSleep { get; set; }
	}
}
