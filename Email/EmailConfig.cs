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
