
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

using System.Collections.Concurrent;

namespace Assistant.Modules.Interfaces.EmailClientInterfaces {

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
