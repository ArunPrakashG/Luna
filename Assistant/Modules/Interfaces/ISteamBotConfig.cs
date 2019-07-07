using System.Collections.Generic;
using static HomeAssistant.AssistantCore.Enums;

namespace HomeAssistant.Modules.Interfaces {

	public interface ISteamBotConfig {

		/// <summary>
		/// The steam id
		/// </summary>
		/// <value></value>
		string SteamID { get; set; }

		/// <summary>
		/// The steam password
		/// </summary>
		/// <value></value>
		string SteamPass { get; set; }

		/// <summary>
		/// Enable or disable the bot instance
		/// </summary>
		/// <value></value>
		bool Enabled { get; set; }

		/// <summary>
		/// Enable chat logging to steam
		/// </summary>
		/// <value></value>
		bool SteamChatLogger { get; set; }

		/// <summary>
		/// Automatically scan recevied messages for spam links etc and remove spammers
		/// </summary>
		/// <value></value>
		bool RemoveSpammers { get; set; }

		/// <summary>
		/// Automatically accept friend requests
		/// </summary>
		/// <value></value>
		bool AcceptFriends { get; set; }

		/// <summary>
		/// Automatically decline steam group invites
		/// </summary>
		/// <value></value>
		bool DeclineGroupInvites { get; set; }

		/// <summary>
		/// Automatically reply the specified text[] to the requester when a friend request gets accepted
		/// </summary>
		/// <value></value>
		List<string> ReplyOnAdd { get; set; }

		/// <summary>
		/// Automatically reply the specifid text[] to the sender every 30 minutes
		/// </summary>
		/// <value></value>
		List<string> ChatResponses { get; set; }

		/// <summary>
		/// Custom text to set in profile while hour boosting or farming or idle
		/// </summary>
		/// <value></value>
		List<string> CustomText { get; set; }

		/// <summary>
		/// The app ids to hour boost
		/// </summary>
		/// <value></value>
		HashSet<uint> GamesToPlay { get; set; }

		/// <summary>
		/// Steam parental pin of the account if there is any
		/// </summary>
		/// <value></value>
		string SteamParentalPin { get; set; }

		/// <summary>
		/// Boolean value indicating if the boosting should be done in offline mode
		/// </summary>
		/// <value></value>
		bool OfflineConnection { get; set; }

		/// <summary>
		/// Permission level for steam accounts
		/// </summary>
		/// <value>ulong value indicating the steam 64 id of the controller, SteamPermissionLevels enum indicating the permission level of the specified steam 64 id</value>
		Dictionary<ulong, SteamPermissionLevels> PermissionLevel { get; set; }
	}
}
