using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Modules.Interfaces {
	public interface IEmailConfig {
		string EmailID { get; set; }
		string EmailPASS { get; set; }
		bool MarkAllMessagesAsRead { get; set; }
		bool MuteNotifications { get; set; }
		string AutoReplyText { get; set; }
		bool DownloadEmails { get; set; }
		bool Enabled { get; set; }
		bool ImapNotifications { get; set; }
		string NotificationSoundPath { get; set; }
		ConcurrentDictionary<bool, string> AutoForwardEmails { get; set; }
	}
}
