using AssistantCore;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Email {
	public class EmailBot : IEmailBot {
		public class ReceviedMessageData : IReceviedMessageDuringIdle {

			public IMessageSummary Message { get; set; }

			public uint UniqueId { get; set; }

			public bool MarkAsRead { get; set; }

			public bool MarkedAsDeleted { get; set; }

			public DateTime ArrivedTime { get; set; }
		}

		public string GmailId { get; set; }
		private string GmailPass { get; set; }
		private string UniqueAccountId;
		private ImapClient IdleClient;
		private ImapClient HelperClient;
		private int InboxMessagesCount { get; set; }
		public bool IsAccountLoaded { get; set; }
		public List<IReceviedMessageDuringIdle> MessagesArrivedDuringIdle { get; set; } = new List<IReceviedMessageDuringIdle>();
		private IEmailConfig MailConfig;
		private Logger BotLogger;
		private IEmailClient EmailHandler;

		private CancellationTokenSource ImapTokenSource { get; set; }
		private CancellationTokenSource ImapCancelTokenSource { get; set; }

		private bool IsIdleCancelRequested { get; set; }

		public async Task<(bool, IEmailBot)> RegisterBot(Logger botLogger, IEmailClient mailHandler, IEmailConfig botConfig, ImapClient coreClient, ImapClient helperClient, string botUniqueId) {
			BotLogger = botLogger ?? throw new ArgumentNullException("botLogger is null");
			MailConfig = botConfig ?? throw new ArgumentNullException("botConfig is null");
			IdleClient = coreClient ?? throw new ArgumentNullException("coreClient is null");
			EmailHandler = mailHandler ?? throw new ArgumentNullException("Email handler is null");
			HelperClient = helperClient ?? throw new ArgumentNullException("helperClient is null");
			UniqueAccountId = botUniqueId ?? throw new ArgumentNullException("botUniqueId is null");
			GmailId = MailConfig.EmailID ?? throw new Exception("email id is either empty or null.");
			GmailPass = MailConfig.EmailPass ?? throw new Exception("email password is either empty or null");
			IsAccountLoaded = false;

			if (MailConfig.ImapNotifications) {
				if (await InitImapNotifications(false).ConfigureAwait(false)) {
				}
			}

			return (true, this);
		}

		private async Task ReconnectImapClient(bool withImapIdle = true) {
			if (IdleClient != null) {
				if (IdleClient.IsConnected) {
					if (IdleClient.IsIdle) {
						StopImapIdle();
					}
					else {
						lock (IdleClient.SyncRoot) {
							IdleClient.Disconnect(true);
							BotLogger.Log("Disconnected IMAP client (ReconnectImapClient())", Enums.LogLevels.Trace);
						}
					}
				}
			}

			IsAccountLoaded = false;
			IdleClient = new ImapClient {
				ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
			};

			BotLogger.Log("Reconnecting to imap server (ReconnectImapClient())", Enums.LogLevels.Trace);
			IdleClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
			IdleClient.Authenticate(GmailId, GmailPass);
			IdleClient.Inbox.Open(FolderAccess.ReadWrite);
			if (IdleClient.IsConnected && IdleClient.IsAuthenticated) {
				BotLogger.Log("Re-established the client connection!");
				IsAccountLoaded = true;

				if (withImapIdle) {
					if (await InitImapNotifications(false).ConfigureAwait(false)) {
					}
				}
			}
		}

		private async Task<bool> InitImapNotifications(bool idleReconnect) {
			if (!MailConfig.Enabled) {
				BotLogger.Log("This account has been disabled in the config file.");
				return false;
			}

			if (!MailConfig.ImapNotifications) {
				BotLogger.Log("Imap notification service is disabled on this account.");
				return false;
			}

			InboxMessagesCount = 0;

			if (IdleClient == null) {
				BotLogger.Log("Fatal error has occured. Idle client isn't initiated.", Enums.LogLevels.Error);
				return false;
			}

			if (!IdleClient.IsConnected || !IdleClient.IsAuthenticated) {
				BotLogger.Log("Account isn't connected or authenticated. failure in registration.", Enums.LogLevels.Warn);
				return false;
			}

			InboxMessagesCount = IdleClient.Inbox.Count;
			BotLogger.Log($"There are {InboxMessagesCount} messages in inbox folder. Starting imap idle...", Enums.LogLevels.Trace);
			IsAccountLoaded = true;
			IsIdleCancelRequested = false;

			IdleClient.Inbox.MessageExpunged += OnMessageExpunged;
			IdleClient.Inbox.CountChanged += OnInboxCountChanged;

			ImapTokenSource?.Dispose();
			ImapCancelTokenSource?.Dispose();
			ImapTokenSource = new CancellationTokenSource();
			ImapCancelTokenSource = new CancellationTokenSource();

			ImapTokenSource.Token.Register(async () => {
				if (!IsIdleCancelRequested) {
					BotLogger.Log("Restarting imap idle as connection got disconnected", Enums.LogLevels.Trace);
					IdleClient.Inbox.MessageExpunged -= OnMessageExpunged;
					IdleClient.Inbox.CountChanged -= OnInboxCountChanged;
					await InitImapNotifications(true).ConfigureAwait(false);
				}
			});

			Helpers.InBackground(async () => {
				if (IdleClient.Capabilities.HasFlag(ImapCapabilities.Idle)) {
					ImapTokenSource.CancelAfter(TimeSpan.FromMinutes(9));
					try {
						lock (IdleClient.SyncRoot) {
							BotLogger.Log($"Mail notification service started for {GmailId}", idleReconnect ? Enums.LogLevels.Trace : Enums.LogLevels.Info);
							IdleClient.Idle(ImapTokenSource.Token, ImapCancelTokenSource.Token);
						}
					}
					catch (OperationCanceledException) {
						BotLogger.Log("Idle cancelled.", Enums.LogLevels.Trace);
						//idle cancelled
					}
					catch (IOException) {
						if (Email.ConfigRoot.EnableImapIdleWorkaround) {
							BotLogger.Log("Starting mobile hotspot workaround...", Enums.LogLevels.Warn);
							if (!IsIdleCancelRequested) {
								BotLogger.Log("Restarting imap idle as connection got disconnected", Enums.LogLevels.Trace);
								await ReconnectImapClient(true).ConfigureAwait(false);
							}
						}
						//mobile hotspot error
					}
				}
				else {
					BotLogger.Log("Mail server doesn't support imap idle.", Enums.LogLevels.Warn);
					return;
				}
			}, true);

			await Task.Delay(500).ConfigureAwait(false);
			return true;
		}

		public void StopImapIdle() {
			if (ImapCancelTokenSource != null) {
				ImapCancelTokenSource.Cancel();
				IsIdleCancelRequested = true;
				BotLogger.Log("IDLE token cancel requested.", Enums.LogLevels.Trace);
			}
		}

		private async void OnInboxCountChanged(object sender, EventArgs e) {
			ImapFolder folder = (ImapFolder) sender;
			BotLogger.Log($"The number of messages in {folder.Name} has changed.", Enums.LogLevels.Trace);

			if (folder.Count <= InboxMessagesCount) {
				return;
			}

			BotLogger.Log($"{folder.Count - InboxMessagesCount} new message(s) have arrived.");

			if (!MailConfig.MuteNotifications && MailConfig.ImapNotifications) {
				Helpers.PlayNotification(Enums.NotificationContext.Imap);
			}

			StopImapIdle();

			if (Monitor.TryEnter(IdleClient.SyncRoot, TimeSpan.FromSeconds(20)) == true) {
				await ReconnectImapClient(false).ConfigureAwait(false);
				await OnMessageArrived().ConfigureAwait(false);
				if (await InitImapNotifications(false).ConfigureAwait(false)) {
				}
			}
			else {
				BotLogger.Log("failed to secure the lock object at the given time of 20 seconds.", Enums.LogLevels.Error);
				return;
			}
		}

		private async Task OnMessageArrived() {
			while (true) {
				if (!IdleClient.IsIdle) {
					BotLogger.Log("Client imap idling has been successfully stopped!", Enums.LogLevels.Trace);
					break;
				}

				BotLogger.Log("Waiting for client to shutdown idling connection...", Enums.LogLevels.Trace);
			}

			List<IMessageSummary> messages;
			if ((IdleClient != null && IdleClient.IsConnected) && IdleClient.Inbox.Count > InboxMessagesCount) {
				if (!IdleClient.IsConnected) {
					return;
				}

				lock (IdleClient.Inbox.SyncRoot) {
					messages = IdleClient.Inbox.Fetch(InboxMessagesCount, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).ToList();
					BotLogger.Log("New message(s) have been fetched to local cache.", Enums.LogLevels.Trace);
				}

				IMessageSummary latestMessage = null;

				BotLogger.Log("Searching for latest message in local cache...", Enums.LogLevels.Trace);
				foreach (IMessageSummary msg in messages) {
					if (MessagesArrivedDuringIdle.Count <= 0) {
						latestMessage = msg;
						BotLogger.Log("Processed latest message. (No message in local cache, new message is the latest message)", Enums.LogLevels.Trace);
						BotLogger.Log($"{latestMessage.Envelope.Sender.FirstOrDefault()?.Name} / {latestMessage.Envelope.Subject}");
						Helpers.InBackgroundThread(
							() => TTSService.SpeakText($"You got an email from {latestMessage.Envelope.Sender.FirstOrDefault()?.Name} with subject {latestMessage.Envelope.Subject}",
								Enums.SpeechContext.Custom), "TTS Service");
						break;
					}

					foreach (ReceviedMessageData msgdata in MessagesArrivedDuringIdle) {
						if (msg.UniqueId.Id != msgdata.UniqueId) {
							latestMessage = msg;
							BotLogger.Log("Processed latest message.", Enums.LogLevels.Trace);
							BotLogger.Log(
								$"{latestMessage.Envelope.Sender.FirstOrDefault()?.Name} / {latestMessage.Envelope.Subject}");
							IMessageSummary message = latestMessage;
							Helpers.InBackgroundThread(
								() => TTSService.SpeakText(
									$"You got an email from {message.Envelope.Sender.FirstOrDefault()?.Name} with subject {message.Envelope.Subject}",
									Enums.SpeechContext.Custom), "TTS Service");
							break;
						}
					}
				}

				await Task.Delay(500).ConfigureAwait(false);

				foreach (IMessageSummary message in messages) {
					MessagesArrivedDuringIdle.Add(new ReceviedMessageData {
						UniqueId = message.UniqueId.Id,
						Message = message,
						MarkAsRead = false,
						MarkedAsDeleted = false,
						ArrivedTime = DateTime.Now
					});
					BotLogger.Log("updated local arrived message(s) cache with a new message.", Enums.LogLevels.Trace);
				}

				if ((!string.IsNullOrEmpty(MailConfig.AutoReplyText) || !string.IsNullOrWhiteSpace(MailConfig.AutoReplyText)) && latestMessage != null) {
					MimeMessage msg = IdleClient.Inbox.GetMessage(latestMessage.UniqueId);

					if (msg == null) {
						return;
					}

					AutoReplyEmail(msg, MailConfig.AutoReplyText);
					BotLogger.Log($"Successfully send auto reply message to {msg.Sender.Address}");
				}
			}
			else {
				BotLogger.Log("Either idle client not connected or no new messages.", Enums.LogLevels.Trace);
			}
		}

		private void AutoReplyEmail(MimeMessage msg, string replyBody) {
			_ = replyBody ?? throw new ArgumentNullException("Body is null!");
			_ = msg ?? throw new ArgumentNullException("Message is null!");

			try {
				string ReplyTextFormat = $"Reply to Previous Message with Subject: {msg.Subject}\n{replyBody}\n\n\nThank you, have a good day!";
				BotLogger.Log($"Sending Auto Reply to {msg.Sender.Address}");
				MailMessage Message = new MailMessage();
				SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
				Message.From = new MailAddress(MailConfig.EmailID);
				Message.To.Add(msg.Sender.Address);
				Message.Subject = $"RE: {msg.Subject}";
				Message.Body = ReplyTextFormat;
				SmtpServer.Port = 587;
				SmtpServer.Credentials = new NetworkCredential(GmailId, GmailPass);
				SmtpServer.EnableSsl = true;
				SmtpServer.Send(Message);
				SmtpServer.Dispose();
				Message.Dispose();
				BotLogger.Log($"Successfully Send Auto-Reply to {msg.From.Mailboxes.First().Address}");
			}
			catch (ArgumentNullException) {
				BotLogger.Log($"One or more arguments are null or empty. cannot send auto reply email to {msg.Sender.Address}", Enums.LogLevels.Warn);
			}
			catch (InvalidOperationException) {
				BotLogger.Log($"Invalid operation exception thrown. cannot send auto reply email to {msg.Sender.Address}", Enums.LogLevels.Error);
			}
			catch (SmtpException) {
				BotLogger.Log("SMTP Exception throw, please check the credentials if they are correct.", Enums.LogLevels.Warn);
			}
			catch (Exception e) {
				BotLogger.Log(e);
			}
		}

		private void OnMessageExpunged(object sender, MessageEventArgs e) {
			if (e.Index >= InboxMessagesCount) {
				return;
			}

			InboxMessagesCount--;
			BotLogger.Log("A message has been expunged, inbox messages count has been reduced by one.", Enums.LogLevels.Trace);
		}

		public void Dispose(bool force) {
			if (force) {
				ImapCancelTokenSource?.Cancel();

				IsAccountLoaded = false;

				if (IdleClient != null) {
					IdleClient.Dispose();
				}
			}
			else {
				StopImapIdle();
				ImapCancelTokenSource?.Cancel();
				BotLogger.Log("Waiting for IDLE Client to release the SyncRoot lock...");
				if (Monitor.TryEnter(IdleClient.SyncRoot, TimeSpan.FromSeconds(20)) == true) {
					if (IdleClient != null) {
						if (IdleClient.IsConnected) {
							if (IdleClient.IsIdle) {
								StopImapIdle();
							}
							else {
								lock (IdleClient.SyncRoot) {
									IdleClient.Disconnect(true);
									BotLogger.Log("Disconnected imap client.", Enums.LogLevels.Trace);
								}
							}
						}
					}

					IsAccountLoaded = false;

					if (IdleClient != null) {
						IdleClient.Dispose();
					}
				}
			}

			if (HelperClient != null) {
				HelperClient.Dispose();
			}

			ImapCancelTokenSource?.Dispose();
			ImapTokenSource?.Dispose();
			EmailHandler.RemoveBotFromCollection(UniqueAccountId);
			BotLogger.Log("Disposed current bot instance of " + UniqueAccountId, Enums.LogLevels.Trace);
		}
	}
}
