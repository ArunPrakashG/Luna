using System.Collections.Concurrent;

namespace Assistant.Modules.Interfaces {

	public interface IEmailConfig {

		/// <summary>
		/// The Email ID
		/// </summary>
		/// <value></value>
		string EmailID { get; set; }

		/// <summary>
		/// The Email password
		/// </summary>
		/// <value></value>
		string EmailPass { get; set; }

		/// <summary>
		/// Mark all messages as seen during timed checks
		/// </summary>
		/// <value></value>
		bool MarkAllMessagesAsRead { get; set; }

		/// <summary>
		/// Mute notification sound
		/// </summary>
		/// <value></value>
		bool MuteNotifications { get; set; }

		/// <summary>
		/// Set text to automatically reply to the sender when your receive a message
		/// </summary>
		/// <value></value>
		string AutoReplyText { get; set; }

		/// <summary>
		/// Download the recevied emails to .eml formate in the assistant directory
		/// </summary>
		/// <value></value>
		bool DownloadEmails { get; set; }

		/// <summary>
		/// Enable the bot instance
		/// </summary>
		/// <value></value>
		bool Enabled { get; set; }

		/// <summary>
		/// Enable IMAP notification service
		/// </summary>
		/// <value></value>
		bool ImapNotifications { get; set; }

		/// <summary>
		/// Path to the custom notification sound (IMAP Notification)
		/// </summary>
		/// <value></value>
		string NotificationSoundPath { get; set; }

		/// <summary>
		/// Automatically forwade the recevied emails to the specified email addresses
		/// </summary>
		/// <value>Boolean value to enable/disable forward for the address at that index, string value representing the email address to forward to.</value>
		ConcurrentDictionary<bool, string> AutoForwardEmails { get; set; }
	}
}
