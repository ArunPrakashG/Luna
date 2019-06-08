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
using System.Net.Sockets;
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

	public class Email : IMail {

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

		private ImapClient Client;
		private ImapClient HelperClient;
		private int InboxMessagesCount = 0;
		private CancellationTokenSource DoneToken;
		public bool IsAccountLoaded = false;
		private bool IsIdleCancelRequested = false;
		private bool IsClientShutdownRequested = false;
		private bool IsReconnectEnabled = false;

		public List<MessageData> MessagesArrivedDuringIdle = new List<MessageData>();
		private readonly EmailConfig MailConfig = new EmailConfig();

		public Email(string uniqueID, EmailConfig mailConfig) {
			MailConfig = mailConfig ?? throw new ArgumentNullException("Mail Config is null!");
			if (string.IsNullOrEmpty(MailConfig.EmailID) || string.IsNullOrWhiteSpace(MailConfig.EmailPASS)
				|| string.IsNullOrEmpty(MailConfig.EmailPASS) || string.IsNullOrWhiteSpace(MailConfig.EmailID)) {
				Logger.Log($"Either gmail or password is empty. cannot proceed with this account... ({MailConfig.EmailID})");
				return;
			}

			GmailID = MailConfig.EmailID ?? throw new NullReferenceException("Email ID is null!");
			GmailPass = MailConfig.EmailPASS ?? throw new NullReferenceException("Email Password is null!");
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

		public void DisposeClient(bool force) {
			IsClientShutdownRequested = true;
			IsIdleCancelRequested = true;
			IsAccountLoaded = false;

			if (force) {
				if (Client != null) {
					Client.Dispose();
					Logger.Log("Forcefully disposed imap client.", LogLevels.Trace);
				}

				if (HelperClient != null) {
					HelperClient.Dispose();
					Logger.Log("Forcefully disposed helper client.", LogLevels.Trace);
				}

				Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountID, out _);
				Logger.Log("Removed email object from client collection", LogLevels.Trace);
			}
		}

		public void DisposeClient() {
			IsClientShutdownRequested = true;
			IsIdleCancelRequested = true;
			IsAccountLoaded = false;

			if (Client == null) {
				return;
			}

			if (Client.IsIdle) {
				StopImapIdle();

				while (true) {
					if (Client.IsIdle) {
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
				Logger.Log("Disposed imap client.", LogLevels.Trace);
			}

			if (HelperClient != null) {
				HelperClient.Dispose();
				Logger.Log("Disposed helper client.", LogLevels.Trace);
			}

			if (Tess.Modules.EmailClientCollection.ContainsKey(UniqueAccountID)) {
				Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountID, out _);
				Logger.Log("Removed email object from client collection.", LogLevels.Trace);
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

			if (HelperClient != null) {
				HelperClient.Dispose();
			}

			Client = new ImapClient();
			InboxMessagesCount = 0;
			IsIdleCancelRequested = false;
			IsClientShutdownRequested = false;
			IsReconnectEnabled = false;

			if (reconnect) {
				Logger.Log("Reconnecting to GMAIL...", LogLevels.Trace);
			}

			int connectionTry = 1;
			bool clientConnected = false;

			while (true) {
				if (connectionTry > 5) {
					Logger.Log($"Connection to gmail account {GmailID} failed after 5 attempts. cannot proceed for this account.", LogLevels.Error);
					IsAccountLoaded = false;
					return;
				}

				try {
					Client.Connect(Constants.GmailHost, Constants.GmailPort, true);
					Client.Authenticate(GmailID, GmailPass);
					Client.Inbox.Open(FolderAccess.ReadWrite);
					InboxMessagesCount = Client.Inbox.Count;

					if (reconnect) {
						Logger.Log("reconnected to gmail sucessfully!", LogLevels.Trace);
					}
					else {
						Logger.Log($"Sucessfully connected and authenticated for {GmailID}.");
					}

					clientConnected = true;
					if (Tess.Modules.EmailClientCollection.ContainsKey(UniqueAccountID)) {
						Logger.Log("Deleting entry with the same unique account id from Client Collection.", LogLevels.Trace);
						Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountID, out _);
					}

					Tess.Modules.EmailClientCollection.TryAdd(UniqueAccountID, this);
					Logger.Log("Added email object to client collection.", LogLevels.Trace);
				}
				catch (AuthenticationException) {
					Logger.Log("Account password must be incorrect. please recheck and re-run!");
					return;
				}
				catch (SocketException) {
					Logger.Log("Network connectivity problem occured, we will retry to connect...", LogLevels.Warn);
				}
				catch (OperationCanceledException) {
					Logger.Log("An operation has been cancelled, failed to connect.", LogLevels.Warn);
				}
				catch (IOException) {
					Logger.Log("IO exception occured. failed to connect.", LogLevels.Warn);
				}
				catch (Exception e) {
					Logger.Log(e, LogLevels.Error);
				}

				if (clientConnected || Client.IsConnected) {
					break;
				}
				else {
					if (connectionTry < 5) {
						Logger.Log($"Could not connect, retrying... ({connectionTry}/5)", LogLevels.Warn);
					}
					connectionTry++;
				}
			}

			Helpers.InBackground(() => {
				HelperClient = new ImapClient();
				HelperClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
				HelperClient.Authenticate(GmailID, GmailPass);
				HelperClient.Inbox.Open(FolderAccess.ReadWrite);
				Logger.Log($"Total messages in inbox: {HelperClient.Inbox.Count} Unread messages: {HelperClient.Inbox.Unread}", LogLevels.Trace);
			});

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

					if (!MailConfig.MuteNotifications && MailConfig.ImapNotifications) {
						Helpers.PlayNotification(NotificationContext.Imap, false);
					}

					IsReconnectEnabled = true;
					StopImapIdle();
				}
			};

			Helpers.InBackground(() => {
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
							if (Client.IsIdle) {
								Logger.Log("Waiting for imap idle client to shutdown...", LogLevels.Trace);
							}
							else {
								//Idle has been sucessfully cancelled
								OnMessageArrived();
								break;
							}
						}

						if (IsClientShutdownRequested && !IsReconnectEnabled) {
							while (true) {
								if (Client.IsIdle) {
									Logger.Log("Waiting for IMAP Client to disconnect idling process...", LogLevels.Trace);
								}
								else {
									if (IsAccountLoaded) {
										lock (Client.SyncRoot) {
											if (Client != null && Client.IsConnected) {
												Client.Disconnect(true);
												Logger.Log("account has been disconnected as shutdown requested.", LogLevels.Trace);
											}
											else {
												Logger.Log("Client is already disposed.", LogLevels.Trace);
											}
										}

										if (Client != null) {
											Client.Dispose();
											Logger.Log("Client has been disposed as shutdown requested.", LogLevels.Trace);
										}

										Logger.Log($"Disconnected and disposed imap and mail client for {GmailID.Split('@').FirstOrDefault().Trim()}");
										break;
									}
									else {
										if(Client == null) {
											Logger.Log("client is already disposed.", LogLevels.Trace);
										}
										else {
											Logger.Log("client is already disconnected.", LogLevels.Trace);

											if (Client != null) {
												Client.Dispose();
												Logger.Log("Client has been disposed as shutdown requested.", LogLevels.Trace);
											}
										}
										
										return;
									}
								}
								Task.Delay(100).Wait();
							}
						}
					}
				}
				catch (ObjectDisposedException) {
					Logger.Log("Client appears to be already disposed.", LogLevels.Trace);
				}
				catch (Exception e) {
					Logger.Log(e.Message, LogLevels.Error);
					return;
				}
			});
		}

		private void OnMessageArrived() {
			if (Client.IsIdle) {
				StopImapIdle();
			}

			while (true) {
				if (!Client.IsIdle) {
					Logger.Log("Client imap idling has been sucessfully stopped!", LogLevels.Trace);
					break;
				}
				else {
					Logger.Log("Waiting for client to shutdown idling connection...", LogLevels.Trace);
				}
				Task.Delay(100).Wait();
			}

			List<IMessageSummary> messages = new List<IMessageSummary>();
			if (Client != null && !IsClientShutdownRequested && Client.Inbox.Count > InboxMessagesCount) {
				if (!Client.IsConnected) { return; }
				lock (Client.SyncRoot) {
					messages = Client.Inbox.Fetch(InboxMessagesCount, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).ToList();
					Logger.Log("Message fetched.", LogLevels.Trace);
				}

				IMessageSummary latestMessage = null;

				if (messages.Count > 0 || messages != null) {
					foreach (IMessageSummary msg in messages) {
						if (MessagesArrivedDuringIdle.Count <= 0) {
							latestMessage = msg;
							Logger.Log("fetched latest message data. (first index of MessageArrivedDuringIdle<> Dictionary)", LogLevels.Trace);
							Logger.Log($"{latestMessage.Envelope.Sender.FirstOrDefault().Name} / {latestMessage.Envelope.Subject}");
							Helpers.InBackgroundThread(() => TTSService.SpeakText($"You got an email from {latestMessage.Envelope.Sender.FirstOrDefault().Name} with subject {latestMessage.Envelope.Subject}", SpeechContext.Custom, true), "TTS Service");
							break;
						}

						foreach (MessageData msgdata in MessagesArrivedDuringIdle) {
							if (msg.UniqueId.Id != msgdata.UniqueId) {
								latestMessage = msg;
								Logger.Log("fetched latest message data.", LogLevels.Trace);
								Logger.Log($"{latestMessage.Envelope.Sender.FirstOrDefault().Name} / {latestMessage.Envelope.Subject}");
								Helpers.InBackgroundThread(() => TTSService.SpeakText($"You got an email from {latestMessage.Envelope.Sender.FirstOrDefault().Name} with subject {latestMessage.Envelope.Subject}", SpeechContext.Custom, true), "TTS Service");
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

				if ((!string.IsNullOrEmpty(MailConfig.AutoReplyText) || !string.IsNullOrWhiteSpace(MailConfig.AutoReplyText)) && latestMessage != null) {
					MimeMessage msg = Client.Inbox.GetMessage(latestMessage.UniqueId);

					if (msg == null) {
						return;
					}

					AutoReplyEmail(msg, MailConfig.AutoReplyText);
					Logger.Log($"Sucessfully send auto reply message to {msg.Sender.Address}");
				}
			}

			if (IsClientShutdownRequested) {
				return;
			}
			else {
				OnIdleStopped(IsClientShutdownRequested);
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
			catch (ArgumentNullException) {
				Logger.Log($"One or more arguments are null or empty. cannot send auto reply email to {msg.Sender.Address}", LogLevels.Warn);
				return;
			}
			catch (InvalidOperationException) {
				Logger.Log($"Invalid operation exception thrown. cannot send auto reply email to {msg.Sender.Address}", LogLevels.Error);
				return;
			}
			catch (SmtpException) {
				Logger.Log("SMTP Exception throw, please check the credentials if they are correct.", LogLevels.Warn);
				return;
			}
			catch (Exception e) {
				Logger.Log(e, LogLevels.Error);
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

		private void OnIdleStopped(bool isShutdownReqested) {
			if (Client.IsIdle) {
				StopImapIdle();
			}

			while (true) {
				if (!Client.IsIdle) {
					Logger.Log("Client imap idling has been sucessfully stopped!", LogLevels.Trace);
					break;
				}
				else {
					Logger.Log("Waiting for client to shutdown idling connection...", LogLevels.Trace);
				}
			}

			lock (Client.SyncRoot) {
				Client.Disconnect(true);
				Logger.Log("Account has been disconnected.", LogLevels.Trace);
				IsAccountLoaded = false;
			}

			if (!isShutdownReqested && !IsClientShutdownRequested) {
				Helpers.InBackground(() => StartImapClient(true));
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
									//TODO: Connection reset by peer error, no fix for mobile networks, added workaround for mobile networks.
									//working normally for normal connections
									//error IOException, socket exception
									if (idle.Client == null) {
										Logger.Log("idle client isnt connected.", LogLevels.Warn);
									}
									else {
										idle.Client.Idle(timeout.Token, idle.CancellationToken);
									}
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
							catch (ServiceNotConnectedException) {
								Logger.Log("An error has occured. IMAP client is not connected to gmail servers. we will attempt to reconnect.", LogLevels.Warn);

								if (idle.Client != null) {
									idle.Client.Dispose();
								}

								DisposeClient();
								Task.Delay(400).Wait();
								StartImapClient(true);
							}
							catch (IOException io) {
								Logger.Log(io.Message, LogLevels.Trace);

								if (idle.Client != null) {
									idle.Client.Dispose();
								}

								if (!IsClientShutdownRequested && IsReconnectEnabled) {
									Logger.Log("Applying connection reset by peer error workaround...", LogLevels.Warn);
									DisposeClient(true);
									StartImapClient(true);
								}
								else {
									Logger.Log("IO Exception thrown during shutdown", LogLevels.Trace);
									Logger.Log("Client shutting down.", LogLevels.Trace);

									if (idle.Client != null) {
										idle.Client.Dispose();
									}

									DisposeClient(true);
								}
								return;
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
				try {
					lock (Mutex) {
						timeout = source;

						if (timeout != null && IsCancellationRequested) {
							timeout.Cancel();
						}
					}
				}
				catch (NullReferenceException) {
					// ignore this as it only happens during exit
				}

			}
		}
	}
}
