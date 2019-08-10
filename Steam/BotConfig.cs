
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
