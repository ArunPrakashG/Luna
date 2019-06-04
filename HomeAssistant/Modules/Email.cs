using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;
using Timer = System.Timers.Timer;

namespace HomeAssistant.Modules {

	public class MessageData {
		public IMessageSummary Message { get; set; }
		public uint UniqueId { get; set; }
		public bool MarkAsRead { get; set; }
		public bool MarkedAsDeleted { get; set; }
		public DateTime ArrivedTime { get; set; }
	}

	public class Email : IModuleBase, IMail {
		//TODO
		//IMAP Client for gmail
		//send emails
		//imap idle service for push notificatons on new mail
		//delete or edit messages
		//mark messages as seen
		//search messages

		private readonly Logger Logger;
		private string GmailID { get; set; }
		private string GmailPass { get; set; }
		public string UniqueAccountID { get; set; }
		public Version Version { get; set; }
		public string ModuleName { get; set; }
		private ImapClient Client;
		private ImapClient HelperClient;
		private int InboxMessagesCount = 0;
		private CancellationTokenSource DoneToken;
		public bool IsAccountLoaded = false;
		private bool IsIdleCancelRequested = false;
		private bool IsClientShutdownRequested = false;
		private bool ClientShutdownSucessfull = false;
		private bool IdleCancelledSucessfully = false;
		private bool IsClientDisconnected = false;
		private bool IsClientInIdle => Client.IsIdle;
		public List<MessageData> MessagesArrivedDuringIdle = new List<MessageData>();
		private EmailConfig MailConfig = new EmailConfig();

		public void OnModuleStarted() {
		}

		public void OnModuleShutdown() {
			DisposeClient();
		}

		public Email(string uniqueID, EmailConfig mailConfig) {
			MailConfig = mailConfig ?? throw new ArgumentNullException("Mail Config is null!");
			if (string.IsNullOrEmpty(MailConfig.EmailID) || string.IsNullOrWhiteSpace(MailConfig.EmailPASS)
				|| string.IsNullOrEmpty(MailConfig.EmailPASS) || string.IsNullOrWhiteSpace(MailConfig.EmailID)) {
				Logger.Log($"Either gmail or password is empty. cannot proceed with this account... ({MailConfig.EmailID})");
				return;
			}

			GmailID = MailConfig.EmailID ?? throw new NullReferenceException("Email ID is null!");
			GmailPass = MailConfig.EmailPASS ?? throw new NullReferenceException("Email Password is null!");
			Version = new Version("1.0.0.0");
			UniqueAccountID = uniqueID ?? MailConfig.EmailID.Split('@').FirstOrDefault().Trim();
			Logger = new Logger($"{UniqueAccountID} | {MailConfig.EmailID.Split('@').FirstOrDefault().Trim()}");
		}

		public bool SendEmail(string toName, string to, string subject, string body, string[] attachmentPaths = null) {
			MimeMessage Message = new MimeMessage();
			Message.From.Add(new MailboxAddress("TESS", GmailID));
			Message.To.Add(new MailboxAddress(toName, to));
			Message.Subject = subject;

			BodyBuilder builder = new BodyBuilder {
				TextBody = body
			};

			if (attachmentPaths.Count() > 0) {
				foreach (string path in attachmentPaths) {
					if (!File.Exists(path)) {
						continue;
					}

					builder.Attachments.Add(path);
				}
			}

			Message.Body = builder.ToMessageBody();

			try {
				MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
				client.Connect(Constants.SMTPHost, Constants.SMPTPort, true);
				client.Authenticate(GmailID, GmailPass);
				client.Send(Message);
				client.Disconnect(true);
				Logger.Log($"Sucessfully send email to {to}");
				return true;
			}
			catch (Exception e) {
				Logger.Log("Send Mail Failed : " + e.Message, LogLevels.Warn);
				return false;
			}
		}

		public void DisposeClient() {
			IsClientShutdownRequested = true;
			IsIdleCancelRequested = true;
			if (!IdleCancelledSucessfully) {
				StopImapIdle();

				Task.Delay(100).Wait();

				while (true) {
					if (!IdleCancelledSucessfully) {
						Logger.Log("Waiting for IMAP Client to disconnect idling process...", LogLevels.Trace);
					}
					else {
						Logger.Log("IMAP Idle has been stopped.", LogLevels.Trace);
						break;
					}
					Task.Delay(100).Wait();
				}
			}

			if (Client != null) {
				Client.Dispose();
				IsClientDisconnected = true;
				ClientShutdownSucessfull = true;
			}
		}

		public void StopImapIdle() => IsIdleCancelRequested = true;

		public void MarkMessageAsSeen() {
			if (!HelperClient.IsConnected) {
				Logger.Log("Cannot process MarkMessageAsSeen as Helper Client is offline.");
				return;
			}

			List<UniqueId> uids = HelperClient.Inbox.Search(SearchQuery.NotSeen).ToList();

			if (uids.Count <= 0) {
				Logger.Log("No messages to mark as seen.");
				return;
			}

			HelperClient.Inbox.AddFlags(uids, MessageFlags.Seen, true);
			Logger.Log($"Sucessfully marked {uids.Count} messages as seen!");
		}

		public void MarkMessageAsSeen(uint messageid) {
			if (!HelperClient.IsConnected) {
				Logger.Log("Cannot process MarkMessageAsSeen as Helper Client is offline.");
				return;
			}

			List<UniqueId> uids = HelperClient.Inbox.Search(SearchQuery.NotSeen).ToList();

			if (uids.Count <= 0) {
				Logger.Log("No messages to mark as seen.");
				return;
			}

			foreach (UniqueId id in uids) {
				if (messageid.Equals(id.Id)) {
					HelperClient.Inbox.AddFlags(id, MessageFlags.Seen, true);
					Logger.Log($"Sucessfully marked {id} message as seen!");
					return;
				}
			}
		}

		public void StartImapClient(bool reconnect) {
			if (!MailConfig.Enabled) {
				Logger.Log("This account has been disabled in the config file.");
				return;
			}

			if (!MailConfig.ImapNotifications) {
				Logger.Log("IDLE Service is disabled in this account.");
				return;
			}

			if (Client != null) {
				Client.Dispose();
			}

			Client = new ImapClient();
			InboxMessagesCount = 0;
			IdleCancelledSucessfully = false;
			IsIdleCancelRequested = false;
			IsClientShutdownRequested = false;
			ClientShutdownSucessfull = false;
			IsClientDisconnected = false;

			if (reconnect) {
				Logger.Log("Reconnecting to GMAIL...", LogLevels.Trace);
			}

			try {
				Client.Connect(Constants.GmailHost, Constants.GmailPort, true);
				Client.Authenticate(GmailID, GmailPass);
			}
			catch (AuthenticationException) {
				Logger.Log("Account password must be incorrect. please recheck and re-run!");
				return;
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Error);
				return;
			}

			Client.Inbox.Open(FolderAccess.ReadWrite);
			InboxMessagesCount = Client.Inbox.Count;
			if (reconnect) {
				Logger.Log("IDLE reconnected sucessfully!");
			}
			else {
				Logger.Log($"Sucessfully connected and authenticated for {GmailID}.");
			}

			HelperClient = new ImapClient();
			HelperClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
			HelperClient.Authenticate(GmailID, GmailPass);
			HelperClient.Inbox.Open(FolderAccess.ReadWrite);
			Logger.Log($"Total messages in inbox: {HelperClient.Inbox.Count} Unread messages: {HelperClient.Inbox.Unread}", LogLevels.Trace);
			IsAccountLoaded = true;

			Client.Inbox.MessageExpunged += (sender, e) => {
				if (e.Index < InboxMessagesCount) {
					InboxMessagesCount--;
					Logger.Log("A message has been expunged, inbox count has been reduced by one.", LogLevels.Trace);
				}
			};

			Client.Inbox.CountChanged += (sender, e) => {
				ImapFolder folder = (ImapFolder) sender;
				Logger.Log($"The number of messages in {folder.Name} has changed.", LogLevels.Trace);

				if (folder.Count > InboxMessagesCount) {
					Logger.Log($"{folder.Count - InboxMessagesCount} new message(s) have arrived.");
					StopImapIdle();
					OnMessageArrived();
				}
			};

			Helpers.InBackgroundThread(() => {
				try {
					using (DoneToken = new CancellationTokenSource()) {
						Thread thread = new Thread(IdleLoop);
						thread.Start(new IdleState(Client, DoneToken.Token));

						while (true) {
							if (IsIdleCancelRequested) {
								break;
							}
							Task.Delay(100).Wait();
						}

						DoneToken.Cancel();
						thread.Join();

						while (true) {
							if (IsClientInIdle) {
								Logger.Log("Waiting for imap idle client to shutdown...", LogLevels.Trace);
							}
							else {
								IdleCancelledSucessfully = true;
								break;
							}
						}

						if (IsClientShutdownRequested && !ClientShutdownSucessfull) {
							while (true) {
								if (IsClientInIdle) {
									Logger.Log("Waiting for IMAP Client to disconnect idling process...", LogLevels.Trace);
								}
								else {
									if (IsAccountLoaded) {
										lock (Client.SyncRoot) {
											Client.Disconnect(true);
											IsClientDisconnected = true;

											if (Client != null) {
												Client.Dispose();
											}

											Logger.Log($"Disconnected and disposed imap and mail client for {GmailID.Split('@').FirstOrDefault().Trim()}");
											break;
										}
									}
									else {
										return;
									}
								}
								Task.Delay(100).Wait();
							}
						}

						if (IsClientShutdownRequested || IsClientDisconnected) {
							return;
						}
						else {
							OnIdleStopped(IsClientShutdownRequested);
						}
					}
				}
				catch (Exception e) {
					Logger.Log(e, ExceptionLogLevels.Error);
					return;
				}
			}, "Idle Thread");
		}

		private void OnMessageArrived() {
			if (IsClientInIdle) {
				StopImapIdle();
			}

			while (true) {
				if (!IsClientInIdle) {
					Logger.Log("Client imap idling has been sucessfully stopped!", LogLevels.Trace);
					break;
				}
				else {
					Logger.Log("Waiting for client to shutdown idling connection...", LogLevels.Trace);
				}
			}

			List<IMessageSummary> messages = new List<IMessageSummary>();
			if (Client.Inbox.Count > InboxMessagesCount) {
				lock (HelperClient.SyncRoot) {
					messages = HelperClient.Inbox.Fetch(InboxMessagesCount, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).ToList();
					Logger.Log("Message fetched.", LogLevels.Trace);
				}

				if (!MailConfig.MuteNotifications && MailConfig.ImapNotifications) {
					Helpers.PlayNotification(NotificationContext.Imap);					
				}

				IMessageSummary latestMessage = null;

				if (messages.Count > 0 || messages != null) {
					foreach (IMessageSummary msg in messages) {
						if(MessagesArrivedDuringIdle.Count <= 0) {
							latestMessage = msg;
							break;
						}

						foreach (MessageData msgdata in MessagesArrivedDuringIdle) {
							if (msg.UniqueId.Id != msgdata.UniqueId) {
								latestMessage = msg;
								Logger.Log("fetched latest message data.", LogLevels.Trace);
								break;
							}
						}
					}
				}

				if (messages.Count > 0 || messages != null) {
					foreach (IMessageSummary message in messages) {
						MessagesArrivedDuringIdle.Add(new MessageData() {
							UniqueId = message.UniqueId.Id,
							Message = message,
							MarkAsRead = false,
							MarkedAsDeleted = false,
							ArrivedTime = DateTime.Now
						});
						Logger.Log("Added a new messageData() object to messagesArrivedDuringIdle.", LogLevels.Trace);
					}
				}

				if (MailConfig.AutoReplyText != null && latestMessage != null) {
					HelperClient.Inbox.Open(FolderAccess.ReadWrite);
					MimeMessage msg = HelperClient.Inbox.GetMessage(latestMessage.UniqueId);
					AutoReplyEmail(msg, MailConfig.AutoReplyText);
					Logger.Log($"Sucessfully send auto reply message to {msg.Sender.Address}");
				}
			}
		}

		private void AutoReplyEmail(MimeMessage msg, string replyBody) {
			_ = replyBody ?? throw new ArgumentNullException("Body is null!");
			_ = msg ?? throw new ArgumentNullException("Message is null!");

			try {
				string ReplyTextFormat = $"Reply to Previous Message with Subject: {msg.Subject}\n{replyBody}\n\n\nThank you, have a good day!";
				Logger.Log($"Sending Auto Reply to {msg.Sender.Address}");
				MailMessage Message = new MailMessage();
				SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
				Message.From = new MailAddress(MailConfig.EmailID);
				Message.To.Add(msg.Sender.Address);
				Message.Subject = $"RE: {msg.Subject}";
				Message.Body = ReplyTextFormat;
				SmtpServer.Port = 587;
				SmtpServer.Credentials = new NetworkCredential(GmailID, GmailPass);
				SmtpServer.EnableSsl = true;
				SmtpServer.Send(Message);
				SmtpServer.Dispose();
				Message.Dispose();
				Logger.Log($"Successfully Send Auto-Reply to {msg.From.Mailboxes.First().Address}");
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Error);
				return;
			}
		}

		public void SendEmail(string to, string subject, string body, string[] attachments) {
			_ = to ?? throw new ArgumentNullException("Email to is null.");
			_ = subject ?? throw new ArgumentNullException("Subject is null.");
			_ = body ?? throw new ArgumentNullException("Body is null.");
			MailMessage Message = new MailMessage();
			System.Net.Mail.SmtpClient SmtpServer = new System.Net.Mail.SmtpClient("smtp.gmail.com");
			Message.From = new MailAddress(MailConfig.EmailID);
			Message.To.Add(to);
			Message.Subject = subject;
			Message.Body = body;

			if (attachments != null && attachments.Count() > 0) {
				foreach (string x in attachments) {
					if (File.Exists(x)) {
						Message.Attachments.Add(new Attachment(x));
					}
				}
			}

			SmtpServer.Port = 587;
			SmtpServer.Credentials = new NetworkCredential(MailConfig.EmailID, MailConfig.EmailPASS);
			SmtpServer.EnableSsl = true;
			SmtpServer.Send(Message);
			SmtpServer.Dispose();
			Message.Dispose();
			Logger.Log($"Successfully send email to {to}");
		}

		private void OnIdleStopped(bool IsShutdownReqested) {
			if (IsClientInIdle) {
				StopImapIdle();
			}

			while (true) {
				if (!IsClientInIdle) {
					Logger.Log("Client imap idling has been sucessfully stopped!", LogLevels.Trace);
					break;
				}
				else {
					Logger.Log("Waiting for client to shutdown idling connection...", LogLevels.Trace);
				}
			}

			lock (Client.SyncRoot) {
				Client.Disconnect(true);
				IsClientDisconnected = true;
				IsAccountLoaded = false;
			}

			if (!IsShutdownReqested && !IsClientShutdownRequested) {
				StartImapClient(true);
			}
		}

		private void IdleLoop(object state) {
			IdleState idle = (IdleState) state;
			lock (idle.Client.SyncRoot) {
				while (!idle.IsCancellationRequested) {
					using (CancellationTokenSource timeout = new CancellationTokenSource()) {
						using (Timer timer = new Timer(9 * 60 * 1000)) {
							timer.Elapsed += (sender, e) => timeout.Cancel();
							timer.AutoReset = false;
							timer.Enabled = true;

							try {
								idle.SetTimeoutSource(timeout);

								if (idle.Client.Capabilities.HasFlag(ImapCapabilities.Idle)) {
									idle.Client.Idle(timeout.Token, idle.CancellationToken);
								}
								else {
									Logger.Log("Issuing NoOp command to IMAP servers...");
									idle.Client.NoOp(idle.CancellationToken);
									WaitHandle.WaitAny(new[] { timeout.Token.WaitHandle, idle.CancellationToken.WaitHandle });
									Logger.Log("NoOp completed!");
								}
							}
							catch (OperationCanceledException) {
								break;
							}
							catch (ImapProtocolException) {
								break;
							}
							catch (ImapCommandException) {
								break;
							}
							finally {
								idle.SetTimeoutSource(null);
							}
						}
					}
				}
			}
		}

		private class IdleState {
			private readonly object Mutex = new object();
			private CancellationTokenSource timeout;

			public CancellationToken CancellationToken { get; private set; }

			public CancellationToken DoneToken { get; private set; }

			public ImapClient Client { get; private set; }

			public bool IsCancellationRequested => CancellationToken.IsCancellationRequested || DoneToken.IsCancellationRequested;

			public IdleState(ImapClient client, CancellationToken doneToken, CancellationToken cancellationToken = default) {
				CancellationToken = cancellationToken;
				DoneToken = doneToken;
				Client = client;
				doneToken.Register(CancelTimeout);
			}

			private void CancelTimeout() {
				lock (Mutex) {
					if (timeout != null) {
						timeout.Cancel();
					}
				}
			}

			public void SetTimeoutSource(CancellationTokenSource source) {
				lock (Mutex) {
					timeout = source;

					if (timeout != null && IsCancellationRequested) {
						timeout.Cancel();
					}
				}
			}
		}
	}
}
