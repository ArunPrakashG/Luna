
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System.Collections.Generic;
using Assistant.AssistantCore;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Modules.Interfaces {

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
		/// Login key of the bot
		/// </summary>
		/// <value></value>
		string LoginKey { get; set; }

		/// <summary>
		/// Permission level for steam accounts
		/// </summary>
		/// <value>ulong value indicating the steam 64 id of the controller, SteamPermissionLevels enum indicating the permission level of the specified steam 64 id</value>
		Dictionary<ulong, Enums.SteamPermissionLevels> PermissionLevel { get; set; }
	}
}
