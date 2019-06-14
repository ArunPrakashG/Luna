using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using MailKit;
using MailKit.Net.Imap;
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

namespace HomeAssistant.Modules {
	public class RawMessageData {
		public IMessageSummary Message { get; set; }
		public uint UniqueId { get; set; }
		public bool MarkAsRead { get; set; }
		public bool MarkedAsDeleted { get; set; }
		public DateTime ArrivedTime { get; set; }
	}

	public class Email_ALPHA {

		private readonly Logger Logger;
		private string GmailID { get; set; }
		private string GmailPass { get; set; }
		public string UniqueAccountID { get; set; }
		private readonly ImapClient HelperClient;
		private readonly EmailConfig MailConfig = new EmailConfig();
		private bool IsIdleCancelRequested = false;
		private int InboxMessagesCount = 0;
		public bool IsAccountLoaded = false;
		private bool IsShutdownRequested = false;
		private bool IsImapClientBusy = false;
		private bool IsIdleCancelledSucessfully = false;
		public List<RawMessageData> MessagesArrivedDuringIdle = new List<RawMessageData>();
		private CancellationTokenSource timeoutToken;

		public Email_ALPHA(string uniqueID, EmailConfig mailConfig) {
			MailConfig = mailConfig ?? throw new ArgumentNullException("Mail config is null!");

			if (string.IsNullOrEmpty(MailConfig.EmailID) || string.IsNullOrWhiteSpace(MailConfig.EmailPASS)
				|| string.IsNullOrEmpty(MailConfig.EmailPASS) || string.IsNullOrWhiteSpace(MailConfig.EmailID)) {
				Logger.Log($"Either gmail or password is empty. cannot proceed with this account... ({MailConfig.EmailID})", LogLevels.Warn);
				return;
			}

			GmailID = MailConfig.EmailID ?? throw new NullReferenceException("Email ID is null!");
			GmailPass = MailConfig.EmailPASS ?? throw new NullReferenceException("Email Password is null!");
			UniqueAccountID = uniqueID ?? MailConfig.EmailID.Split('@').FirstOrDefault().Trim();
			Logger = new Logger($"{UniqueAccountID} | {MailConfig.EmailID.Split('@').FirstOrDefault().Trim()}");

			if (!MailConfig.Enabled) {
				Logger.Log("This account has been disabled in the config file.");
				return;
			}
		}

		public void CancelImapIdle() => IsIdleCancelRequested = true;

		private void ShutdownImapClient() => IsShutdownRequested = true;

		public void Dispose() {
			IsIdleCancelRequested = true;
			IsShutdownRequested = true;

			if (HelperClient != null) {
				if (HelperClient.IsConnected) {
					HelperClient.Disconnect(true);
				}
				HelperClient.Dispose();
			}

			if (Tess.Modules.EmailClientCollection.ContainsKey(UniqueAccountID)) {
				Logger.Log("Deleting entry with the same unique account id from Client Collection.", LogLevels.Trace);
				Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountID, out _);
			}
		}

		public void SendEmail(string to, string subject, string body, string[] attachments) {
			_ = to ?? throw new ArgumentNullException("Email to is null.");
			_ = subject ?? throw new ArgumentNullException("Subject is null.");
			_ = body ?? throw new ArgumentNullException("Body is null.");
			MailMessage Message = new MailMessage();
			SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
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

		public void StartImapIdle(bool reconnect) {
			if (!MailConfig.Enabled) {
				Logger.Log("This account has been disabled in the config file.");
				return;
			}

			if (!MailConfig.ImapNotifications) {
				Logger.Log("IDLE Service is disabled in this account.");
				return;
			}

			if (reconnect) {
				Logger.Log("Reconnecting to GMAIL...", LogLevels.Trace);
			}

			int connectionTry = 1;
			IsAccountLoaded = false;
			IsShutdownRequested = false;
			InboxMessagesCount = 0;
			IsImapClientBusy = false;
			IsIdleCancelledSucessfully = false;

			using (ImapClient Client = new ImapClient()) {
				while (true) {
					if (connectionTry > 5) {
						Logger.Log($"Connection to gmail account {GmailID} failed after 5 attempts. cannot proceed for this account.", LogLevels.Error);
						IsAccountLoaded = false;
						return;
					}

					try {
						Client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
						Client.Connect(Constants.GmailHost, Constants.GmailPort, true);
						Client.Authenticate(GmailID, GmailPass);
						Client.Inbox.Open(FolderAccess.ReadOnly);

						if (reconnect) {
							Logger.Log("reconnected to gmail sucessfully!", LogLevels.Trace);
						}
						else {
							Logger.Log($"Sucessfully connected and authenticated for {GmailID}.");
						}

						if (Tess.Modules.EmailClientCollection.ContainsKey(UniqueAccountID)) {
							Logger.Log("Deleting entry with the same unique account id from Client Collection.", LogLevels.Trace);
							Tess.Modules.EmailClientCollection.TryRemove(UniqueAccountID, out _);
						}

						//Tess.Modules.EmailClientCollection.TryAdd(UniqueAccountID, this);
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

					if (IsAccountLoaded || Client.IsConnected) {
						break;
					}
					else {
						if (connectionTry < 5) {
							Logger.Log($"Could not connect, retrying... ({connectionTry}/5)", LogLevels.Warn);
						}
						connectionTry++;
					}
				}

				InboxMessagesCount = Client.Inbox.Count;

				Client.Inbox.MessageExpunged += (sender, e) => {
					ImapFolder folder = (ImapFolder) sender;

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
						CancelImapIdle();

						while (true) {
							if (Client.IsIdle) {
								Logger.Log("Waiting for imap idle client to stop idling...", LogLevels.Trace);
							}
							else {
								break;
							}
						}

						while (true) {
							if (IsIdleCancelledSucessfully) {
								break;
							}
							else {
								Logger.Log("Idle isnt cancelled yet.", LogLevels.Trace);
							}
						}

						IsImapClientBusy = true;

						List<IMessageSummary> messages = new List<IMessageSummary>();
						if (Client != null && Client.Inbox.Count > InboxMessagesCount) {
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

									foreach (RawMessageData msgdata in MessagesArrivedDuringIdle) {
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
									MessagesArrivedDuringIdle.Add(new RawMessageData() {
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

						//OnIdleStopped(IsShutdownRequested);
						IsImapClientBusy = false;
					}
				};

				using (CancellationTokenSource done = new CancellationTokenSource()) {
					Thread thread = new Thread(IdleLoop);
					IdleState idleState = new IdleState(Client, done.Token);
					thread.Start(idleState);

					while (true) {
						if (IsIdleCancelRequested) {
							timeoutToken.Cancel();
							break;
						}
						Task.Delay(50).Wait();
					}

					while (true) {
						if (IsIdleCancelledSucessfully) {
							thread.Join();
							break;
						}
						else {
							Logger.Log("Idle isnt cancelled yet.", LogLevels.Trace);
						}
					}
				}

				while (true) {
					if (!IsImapClientBusy) {
						Client.Disconnect(true);

						if (!IsShutdownRequested) {
							Helpers.InBackground(() => {
								Task.Delay(100).Wait();
								StartImapIdle(true);
							});
						}
					}
				}
			}
		}

		private void IdleLoop(object state) {
			IdleState idle = (IdleState) state;

			lock (idle.Client.SyncRoot) {
				while (!idle.IsCancellationRequested) {
					using (timeoutToken = new CancellationTokenSource(new TimeSpan(0, 9, 0))) {
						try {
							// We set the timeout source so that if the idle.DoneToken is cancelled, it can cancel the timeout
							idle.SetTimeoutSource(timeoutToken);

							if (idle.Client.Capabilities.HasFlag(ImapCapabilities.Idle)) {
								// The Idle() method will not return until the timeout has elapsed or idle.CancellationToken is cancelled
								idle.Client.Idle(timeoutToken.Token, idle.CancellationToken);
								IsIdleCancelledSucessfully = true;
							}
							else {
								// The IMAP server does not support IDLE, so send a NOOP command instead
								idle.Client.NoOp(idle.CancellationToken);

								// Wait for the timeout to elapse or the cancellation token to be cancelled
								WaitHandle.WaitAny(new[] { timeoutToken.Token.WaitHandle, idle.CancellationToken.WaitHandle });
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
							break;
						}
						catch (ImapCommandException) {
							// The IMAP server responded with "NO" or "BAD" to either the IDLE command or the NOOP command.
							// This should never happen... but again, we're catching it for the sake of completeness.
							break;
						}
						catch (ServiceNotConnectedException) {
							Logger.Log("An error has occured. IMAP client is not connected to gmail servers. we will attempt to reconnect.", LogLevels.Warn);

							if (idle.Client != null) {
								idle.Client.Dispose();
							}

							CancelImapIdle();
							Task.Delay(400).Wait();
							StartImapIdle(true);
						}
						catch (IOException io) {
							Logger.Log(io.Message, LogLevels.Trace);

							if (idle.Client != null) {
								idle.Client.Dispose();
							}

							if (!IsShutdownRequested) {
								Logger.Log("Applying connection reset by peer error workaround...", LogLevels.Warn);
								CancelImapIdle();
								StartImapIdle(true);
							}
							else {
								Logger.Log("IO Exception thrown", LogLevels.Trace);
								Logger.Log("Client shutting down.", LogLevels.Trace);

								if (idle.Client != null) {
									idle.Client.Dispose();
								}

								Dispose();
							}
						}
						finally {
							// We're about to Dispose() the timeout source, so set it to null.
							idle.SetTimeoutSource(null);
						}
					}
				}
			}
		}

		private class IdleState {
			private readonly object mutex = new object();
			private CancellationTokenSource timeout;

			/// <summary>
			/// Get the cancellation token.
			/// </summary>
			/// <remarks>
			/// <para>The cancellation token is the brute-force approach to cancelling the IDLE and/or NOOP command.</para>
			/// <para>Using the cancellation token will typically drop the connection to the server and so should
			/// not be used unless the client is in the process of shutting down or otherwise needs to
			/// immediately abort communication with the server.</para>
			/// </remarks>
			/// <value>The cancellation token.</value>
			public CancellationToken CancellationToken { get; private set; }

			/// <summary>
			/// Get the done token.
			/// </summary>
			/// <remarks>
			/// <para>The done token tells the <see cref="Program.IdleLoop"/> that the user has requested to end the loop.</para>
			/// <para>When the done token is cancelled, the <see cref="Program.IdleLoop"/> will gracefully come to an end by
			/// cancelling the timeout and then breaking out of the loop.</para>
			/// </remarks>
			/// <value>The done token.</value>
			public CancellationToken DoneToken { get; private set; }

			/// <summary>
			/// Get the IMAP client.
			/// </summary>
			/// <value>The IMAP client.</value>
			public ImapClient Client { get; private set; }

			/// <summary>
			/// Check whether or not either of the CancellationToken's have been cancelled.
			/// </summary>
			/// <value><c>true</c> if cancellation was requested; otherwise, <c>false</c>.</value>
			public bool IsCancellationRequested => CancellationToken.IsCancellationRequested || DoneToken.IsCancellationRequested;

			/// <summary>
			/// Initializes a new instance of the <see cref="IdleState"/> class.
			/// </summary>
			/// <param name="client">The IMAP client.</param>
			/// <param name="doneToken">The user-controlled 'done' token.</param>
			/// <param name="cancellationToken">The brute-force cancellation token.</param>
			public IdleState(ImapClient client, CancellationToken doneToken, CancellationToken cancellationToken = default(CancellationToken)) {
				CancellationToken = cancellationToken;
				DoneToken = doneToken;
				Client = client;

				// When the user hits a key, end the current timeout as well
				doneToken.Register(CancelTimeout);
			}

			/// <summary>
			/// Cancel the timeout token source, forcing ImapClient.Idle() to gracefully exit.
			/// </summary>
			private void CancelTimeout() {
				lock (mutex) {
					if (timeout != null) {
						timeout.Cancel();
					}
				}
			}

			/// <summary>
			/// Set the timeout source.
			/// </summary>
			/// <param name="source">The timeout source.</param>
			public void SetTimeoutSource(CancellationTokenSource source) {
				lock (mutex) {
					timeout = source;

					if (timeout != null && IsCancellationRequested) {
						timeout.Cancel();
					}
				}
			}
		}
	}
}
