
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

using Assistant.Modules.Interfaces;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Email {

	public class EmailConfigRoot {

		[JsonProperty] public bool EnableImapIdleWorkaround { get; set; } = true;

		[JsonProperty] public bool EnableEmailModule { get; set; } = true;

		[JsonProperty] public ConcurrentDictionary<string, EmailConfig> EmailDetails { get; set; } = new ConcurrentDictionary<string, EmailConfig>();
	}

	public class EmailConfig : IEmailConfig {

		[JsonProperty]
		public string EmailID { get; set; }

		[JsonProperty]
		public string EmailPass { get; set; }

		[JsonProperty]
		public bool MarkAllMessagesAsRead { get; set; } = false;

		[JsonProperty]
		public bool MuteNotifications { get; set; } = false;

		[JsonProperty]
		public string AutoReplyText { get; set; }

		[JsonProperty]
		public bool DownloadEmails { get; set; } = false;

		[JsonProperty]
		public bool Enabled { get; set; } = true;

		[JsonProperty]
		public bool ImapNotifications { get; set; } = true;

		[JsonProperty]
		public string NotificationSoundPath { get; set; }

		[JsonProperty]
		public ConcurrentDictionary<bool, string> AutoForwardEmails { get; set; } = new ConcurrentDictionary<bool, string>();
	}
}
