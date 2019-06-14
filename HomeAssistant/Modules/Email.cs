using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using Unosquare.Swan.Abstractions;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {

	public class EmailBot : IDisposable {
		public class ReceviedMessageData {

			public IMessageSummary Message { get; set; }

			public uint UniqueId { get; set; }

			public bool MarkAsRead { get; set; }

			public bool MarkedAsDeleted { get; set; }

			public DateTime ArrivedTime { get; set; }
		}

		private string GmailId;
		private string GmailPass;
		private string UniqueAccountId;
		private ImapClient IdleClient;
		private ImapClient HelperClient;
		private int InboxMessagesCount;
		public bool IsAccountLoaded;
		public List<ReceviedMessageData> MessagesArrivedDuringIdle = new List<ReceviedMessageData>();
		private EmailConfig MailConfig;
		private Logger BotLogger;
		private (int, Thread) ImapThreadInfo;

		private CancellationToken CancellationToken { get; set; }
		private CancellationToken DoneToken { get; set; }
		private CancellationTokenSource ImapToken { get; set; }
		private CancellationTokenSource Timeout { get; set; }

		private bool IsCancellationRequested => CancellationToken.IsCancellationRequested || DoneToken.IsCancellationRequested;
		private readonly object Mutex = new object();

		private void CancelTimeout() {
			lock (Mutex) {
				Timeout?.Cancel();
			}
		}

		private void SetTimeoutSource(CancellationTokenSource source) {
			lock (Mutex) {
				Timeout = source;

				if (Timeout != null && IsCancellationRequested) {
					Timeout.Cancel();
				}
			}
		}

		private void SetTokenValues(CancellationToken doneToken, CancellationToken cancellationToken = default) {
			CancellationToken = cancellationToken;
			DoneToken = doneToken;
			doneToken.Register(CancelTimeout);
		}

		public async Task<(bool, EmailBot)> RegisterBot(Logger botLogger, EmailConfig botConfig, ImapClient coreClient, ImapClient helperClient, string botUniqueId) {
			BotLogger = botLogger ?? throw new ArgumentNullException("botLogger is null");
			MailConfig = botConfig ?? throw new ArgumentNullException("botConfig is null");
			IdleClient = coreClient ?? throw new ArgumentNullException("coreClient is null");
			HelperClient = helperClient ?? throw new ArgumentNullException("helperClient is null");
			UniqueAccountId = botUniqueId ?? throw new ArgumentNullException("botUniqueId is null");
			GmailId = MailConfig.EmailID ?? throw new Exception("email id is either empty or null.");
			GmailPass = MailConfig.EmailPASS ?? throw new Exception("email password is either empty or null");
			IsAccountLoaded = false;
			AddBotToCollection();

			if (MailConfig.ImapNotifications) {
				_ = await InitImapNotifications().ConfigureAwait(false);
			}

			return (true, this);
		}

		private void AddBotToCollection() {
			if (Tess.Modules.EmailClientCollection.ContainsKey(UniqueAccountId)) {
				BotLogger.Log("Deleting duplicate entry of current instance.", LogLevels.Trace);
				Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountId, out _);
			}

			Tess.Modules.EmailClientCollection.TryAdd(UniqueAccountId, this);
			BotLogger.Log("Added current instance to client collection.", LogLevels.Trace);
		}

		private void RemoveBotFromCollection() {
			if (Tess.Modules.EmailClientCollection.ContainsKey(UniqueAccountId)) {
				BotLogger.Log("Remove current bot instance from collection.", LogLevels.Trace);
				Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountId, out _);
			}
		}

		private async Task<bool> InitImapNotifications() {
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
				BotLogger.Log("Fatal error has occured. Idle client isn't initiated.", LogLevels.Error);
				return false;
			}

			if (!IdleClient.IsConnected || !IdleClient.IsAuthenticated) {
				BotLogger.Log("Account isn't connected or authenticated. failure in registration.");
				return false;
			}

			InboxMessagesCount = IdleClient.Inbox.Count;
			BotLogger.Log($"There are {InboxMessagesCount} messages in inbox folder. starting imap idle...");
			IsAccountLoaded = true;

			IdleClient.Inbox.MessageExpunged += OnMessageExpunged;
			IdleClient.Inbox.CountChanged += OnInboxCountChanged;

			ImapToken = new CancellationTokenSource();
			SetTokenValues(ImapToken.Token);

			ImapToken.Token.ThrowIfCancellationRequested();
			ImapThreadInfo = Helpers.InBackgroundThread(ImapIdleLoop, UniqueAccountId, true);

			await Task.Delay(1000).ConfigureAwait(false);
			BotLogger.Log($"Started notifications service for {GmailId}");
			return true;
		}

		public void StopImapIdle(bool clientDisconnect) {
            ImapToken.Cancel();
			try {
				Task.Factory.StartNew(() => {
					ImapThreadInfo.Item2?.Join();
				});
				ImapToken.Dispose();

				if (!clientDisconnect) {
					return;
				}

				if (IdleClient.IsConnected && IdleClient.IsIdle) {
					while (true) {
						if (!IdleClient.IsIdle) {
							BotLogger.Log("Idling has been stopped.", LogLevels.Trace);
							break;
						}
						BotLogger.Log("Waiting for idle client to stop idling...", LogLevels.Trace);
					}
				}

				lock (IdleClient.SyncRoot) {
					IdleClient.Disconnect(true);
					BotLogger.Log("Imap client has been disconnected.", LogLevels.Trace);
				}
			}
			catch (NullReferenceException) {
				BotLogger.Log("There is no thread with the specified uniqueID", LogLevels.Warn);
			}
			IsAccountLoaded = false;
		}

		private async Task ReconnectImapClient(bool withImapIdle = true) {
			if (IdleClient != null) {
				if (IdleClient.IsConnected) {
					if (IdleClient.IsIdle) {
						StopImapIdle(true);
					}
					else {
						lock (IdleClient.SyncRoot) {
							IdleClient.Disconnect(true);
							BotLogger.Log("Disconnected imap client.", LogLevels.Trace);
						}
					}
				}
			}
			else {
				IsAccountLoaded = false;
				IdleClient = new ImapClient {
					ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
				};

				BotLogger.Log("Reconnecting to imap server...");
				IdleClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
				IdleClient.Authenticate(GmailId, GmailPass);
				IdleClient.Inbox.Open(FolderAccess.ReadWrite);
				if (IdleClient.IsConnected && IdleClient.IsAuthenticated) {
					BotLogger.Log("Reconnected and authenticated!");

					if (withImapIdle) {
						bool imapStarted = await InitImapNotifications().ConfigureAwait(false);

						if (imapStarted) {
							BotLogger.Log("Restarted imap notifications service!");
						}
					}
				}

				IsAccountLoaded = true;
			}
		}

		private async void OnInboxCountChanged(object sender, EventArgs e) {
			ImapFolder folder = (ImapFolder) sender;
			BotLogger.Log($"The number of messages in {folder.Name} has changed.", LogLevels.Trace);

			if (folder.Count <= InboxMessagesCount) {
				return;
			}

			BotLogger.Log($"{folder.Count - InboxMessagesCount} new message(s) have arrived.");

			if (!MailConfig.MuteNotifications && MailConfig.ImapNotifications) {
				Helpers.PlayNotification(NotificationContext.Imap);
			}

			StopImapIdle(true);
			await ReconnectImapClient(false).ConfigureAwait(false);
			await OnMessageArrived().ConfigureAwait(false);
			Helpers.InBackground(async () => {
				bool imapStarted = await InitImapNotifications().ConfigureAwait(false);

				if (imapStarted) {
					BotLogger.Log("Restarted imap notifications service!");
				}
			});
		}

		private async Task OnMessageArrived() {
			while (true) {
				if (!IdleClient.IsIdle) {
					BotLogger.Log("Client imap idling has been successfully stopped!", LogLevels.Trace);
					break;
				}

				BotLogger.Log("Waiting for client to shutdown idling connection...", LogLevels.Trace);
				await Task.Delay(20).ConfigureAwait(false);
			}

			List<IMessageSummary> messages;
			if (IdleClient != null && IdleClient.Inbox.Count > InboxMessagesCount) {
				if (!IdleClient.IsConnected) {
					return;
				}

				lock (IdleClient.SyncRoot) {
					messages = IdleClient.Inbox.Fetch(InboxMessagesCount, -1,
						MessageSummaryItems.Full | MessageSummaryItems.UniqueId).ToList();
					BotLogger.Log("Message fetched.", LogLevels.Trace);
				}

				IMessageSummary latestMessage = null;

				foreach (IMessageSummary msg in messages) {
					if (MessagesArrivedDuringIdle.Count <= 0) {
						latestMessage = msg;
						BotLogger.Log(
							"fetched latest message data. (first index of MessageArrivedDuringIdle<> Dictionary)",
							LogLevels.Trace);
						BotLogger.Log(
							$"{latestMessage.Envelope.Sender.FirstOrDefault()?.Name} / {latestMessage.Envelope.Subject}");
						Helpers.InBackgroundThread(
							() => TTSService.SpeakText(
								$"You got an email from {latestMessage.Envelope.Sender.FirstOrDefault()?.Name} with subject {latestMessage.Envelope.Subject}",
								SpeechContext.Custom), "TTS Service");
						break;
					}

					foreach (ReceviedMessageData msgdata in MessagesArrivedDuringIdle) {
						if (msg.UniqueId.Id != msgdata.UniqueId) {
							latestMessage = msg;
							BotLogger.Log("fetched latest message data.", LogLevels.Trace);
							BotLogger.Log(
								$"{latestMessage.Envelope.Sender.FirstOrDefault()?.Name} / {latestMessage.Envelope.Subject}");
							IMessageSummary message = latestMessage;
							Helpers.InBackgroundThread(
								() => TTSService.SpeakText(
									$"You got an email from {message.Envelope.Sender.FirstOrDefault()?.Name} with subject {message.Envelope.Subject}",
									SpeechContext.Custom), "TTS Service");
							break;
						}
					}
				}

				foreach (IMessageSummary message in messages) {
					MessagesArrivedDuringIdle.Add(new ReceviedMessageData {
						UniqueId = message.UniqueId.Id,
						Message = message,
						MarkAsRead = false,
						MarkedAsDeleted = false,
						ArrivedTime = DateTime.Now
					});
					BotLogger.Log("Added a new messageData() object to messagesArrivedDuringIdle.", LogLevels.Trace);
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
				BotLogger.Log($"One or more arguments are null or empty. cannot send auto reply email to {msg.Sender.Address}", LogLevels.Warn);
			}
			catch (InvalidOperationException) {
				BotLogger.Log($"Invalid operation exception thrown. cannot send auto reply email to {msg.Sender.Address}", LogLevels.Error);
			}
			catch (SmtpException) {
				BotLogger.Log("SMTP Exception throw, please check the credentials if they are correct.", LogLevels.Warn);
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
			BotLogger.Log("A message has been expunged, inbox messages count has been reduced by one.", LogLevels.Trace);
		}

		private void ImapIdleLoop() {
			while (!IsCancellationRequested) {
				Timeout = new CancellationTokenSource(new TimeSpan(0, 9, 0));

				try {
					SetTimeoutSource(Timeout);
					if (IdleClient.Capabilities.HasFlag(ImapCapabilities.Idle)) {
						lock (IdleClient.SyncRoot) {
							IdleClient.Idle(Timeout.Token, CancellationToken);
						}
					}
					else {
						lock (IdleClient.SyncRoot) {
							IdleClient.NoOp(CancellationToken);
						}
						WaitHandle.WaitAny(new[] { Timeout.Token.WaitHandle, CancellationToken.WaitHandle });
					}
				}
				catch (OperationCanceledException) {
					// This means that idle.CancellationToken was cancelled, not the DoneToken nor the timeout.
					break;
				}
				catch (ImapProtocolException) {
					// The IMAP server sent garbage in a response and the ImapClient was unable to deal with it.
					// This should never happen in practice, but it's probably still a good idea to handle it.
					// 
					// Note: an ImapProtocolException almost always results in the ImapClient getting disconnected.
					IsAccountLoaded = false;
					break;
				}
				catch (ImapCommandException) {
					// The IMAP server responded with "NO" or "BAD" to either the IDLE command or the NOOP command.
					// This should never happen... but again, we're catching it for the sake of completeness.
					break;
				}
				catch (SocketException) {
					//Ignore has this only seems to happen during shutdown
					break;
				}
				catch (ServiceNotConnectedException) {
					BotLogger.Log("An error has occured. IMAP client is not connected to gmail servers. we will attempt to reconnect.", LogLevels.Warn);
					IsAccountLoaded = false;
					Helpers.InBackground(async () => await ReconnectImapClient().ConfigureAwait(false));
				}
				catch (IOException) {
					BotLogger.Log("IO Exception thrown. possibly, tess is connected to a mobile hotspot connection.", LogLevels.Warn);
					IsAccountLoaded = false;
					BotLogger.Log("Applying mobile hotspot connection workaround...", LogLevels.Warn);
					Helpers.InBackground(async () => await ReconnectImapClient().ConfigureAwait(false));
				}
				finally {
					// We're about to Dispose() the timeout source, so set it to null.
					SetTimeoutSource(null);
				}
				Timeout?.Dispose();
			}
		}

		public void Dispose() {
			StopImapIdle(true);

			if (IdleClient != null) {
				IdleClient.Dispose();
			}

			if (HelperClient != null) {
				HelperClient.Dispose();
			}

			Timeout?.Dispose();
			ImapToken.Dispose();

			RemoveBotFromCollection();
			BotLogger.Log("Disposed current bot instance of " + UniqueAccountId);
		}
	}

	public class Email {

		private List<EmailBot> BotsCollection = new List<EmailBot>();
		private readonly Logger Logger = new Logger("EMAIL-HANDLER");

		public (bool, EmailBot) InitBot(string uniqueId, EmailConfig botConfig) {
			_ = uniqueId ?? throw new ArgumentNullException("uniqueId is null");
			_ = botConfig ?? throw new ArgumentNullException("botConfig is null");
			_ = botConfig.EmailID ?? throw new ArgumentNullException("email ID is null");
			_ = botConfig.EmailPASS ?? throw new ArgumentNullException("email pass is null");

			int connectionTry = 1;
			int maxConnectionRetry = 5;

			while (true) {
				if (connectionTry > maxConnectionRetry) {
					Logger.Log($"Connection to gmail account {botConfig.EmailID} failed after {maxConnectionRetry} attempts. cannot proceed for this account.", LogLevels.Error);
					return (false, null);
				}

				try {
					Logger BotLogger = new Logger(botConfig.EmailID?.Split('@')?.FirstOrDefault()?.Trim());
					ImapClient BotClient = new ImapClient(new ProtocolLogger(uniqueId + ".txt"));

					BotClient.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
					BotClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
					BotClient.Authenticate(botConfig.EmailID, botConfig.EmailPASS);
					BotClient.Inbox.Open(FolderAccess.ReadWrite);

					Task.Delay(500).Wait();

					ImapClient BotHelperClient = new ImapClient {
						ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
					};

					BotHelperClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
					BotHelperClient.Authenticate(botConfig.EmailID, botConfig.EmailPASS);
					BotHelperClient.Inbox.Open(FolderAccess.ReadWrite);

					EmailBot Bot = new EmailBot();
					(bool, EmailBot) registerResult = Bot.RegisterBot(BotLogger, botConfig, BotClient, BotHelperClient, uniqueId).Result;

					if ((registerResult.Item2.IsAccountLoaded || registerResult.Item1) && Tess.Modules.EmailClientCollection.ContainsKey(uniqueId)) {
						Logger.Log($"Loaded {botConfig.EmailID} mail account.");
						BotsCollection.Add(registerResult.Item2);
						Logger.Log("Added email bot instance to bot collection.");
						return (true, Bot);
					}
				}
				catch (AuthenticationException) {
					Logger.Log($"Account password must be incorrect. we will retry to connect. ({connectionTry}/{maxConnectionRetry})");
				}
				catch (SocketException) {
					Logger.Log($"Network connectivity problem occured, we will retry to connect. ({connectionTry}/{maxConnectionRetry})", LogLevels.Warn);
				}
				catch (OperationCanceledException) {
					Logger.Log($"An operation has been cancelled, we will retry to connect. ({connectionTry}/{maxConnectionRetry})", LogLevels.Warn);
				}
				catch (IOException) {
					Logger.Log($"IO exception occured. we will retry to connect. ({connectionTry}/{maxConnectionRetry})", LogLevels.Warn);
				}
				catch (Exception e) {
					Logger.Log(e, LogLevels.Error);
					return (false, null);
				}
			}
		}
	}
}
