using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;
using Timer = System.Timers.Timer;

namespace HomeAssistant.Modules {

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
		private int InboxMessagesCount;
		private CancellationTokenSource DoneToken;
		public bool IsAccountLoaded = false;
		public bool IsIdleCancelRequested = false;
		public bool IsClientShutdownRequested = false;
		public bool ClientShutdownSucessfull = false;
		public bool IdleCancelledSucessfully = false;
		public bool IsClientDisconnected = false;
		public bool IsClientInIdle => Client.IsIdle;

		public void OnModuleStarted() {
		}

		public void OnModuleShutdown() {
			DisposeClient();
		}

		public Email(string gmailID, string gmailPASS) {
			if (string.IsNullOrEmpty(gmailID) || string.IsNullOrWhiteSpace(gmailID)
				|| string.IsNullOrEmpty(gmailPASS) || string.IsNullOrWhiteSpace(gmailPASS)) {
				Logger.Log("Either gmail or password is empty. cannot proceed with this account..." + " (" + gmailID + "/" + GmailPass + ")");
				return;
			}

			GmailID = gmailID;
			GmailPass = gmailPASS;
			Version = new Version("1.0.0.0");
			UniqueAccountID = GmailID.Split('@').FirstOrDefault();
			Logger = new Logger(UniqueAccountID);
		}

		public bool SendEmail(string toName, string to, string subject, string body, string[] attachmentPaths) {
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
				SmtpClient client = new SmtpClient();
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
						Logger.Log("IMAP Idle has been stopped.");
						break;
					}
					Task.Delay(200).Wait();
				}
			}

			if (Client != null) {
				Client.Dispose();
				IsClientDisconnected = true;
				ClientShutdownSucessfull = true;
			}
		}

		public void StopImapIdle() => IsIdleCancelRequested = true;

		public void StartImapClient(bool reconnect) {
			if (!Program.Config.ImapNotifications) {
				Logger.Log("IMAP Idle is disabled in config file.");
				return;
			}

			if (Client != null) {
				Client.Dispose();
			}

			InboxMessagesCount = 0;

			Client = new ImapClient();
			IdleCancelledSucessfully = false;
			IsIdleCancelRequested = false;
			IsClientShutdownRequested = false;
			ClientShutdownSucessfull = false;
			IsClientDisconnected = false;

			if (reconnect) {
				Logger.Log("Reconnecting to host...");
			}

			try {
				Client.Connect(Constants.GmailHost, Constants.GmailPort, true);
				Client.Authenticate(GmailID, GmailPass);
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Error);
				return;
			}

			Client.Inbox.Open(FolderAccess.ReadWrite);
			InboxMessagesCount = Client.Inbox.Count;
			Logger.Log($"Sucessfully connected and authenticated for {GmailID}. ({InboxMessagesCount} messages found)");
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
					Logger.Log($"{folder.Count - InboxMessagesCount} new message(s) have arrived.", LogLevels.Info);
					StopImapIdle();
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
								Logger.Log("Waiting for imap idle client to shutdown...");
							}
							else {
								IdleCancelledSucessfully = true;
								break;
							}
						}

						if (IsClientShutdownRequested && !ClientShutdownSucessfull) {
							while (true) {
								if (IsClientInIdle) {
									Logger.Log("Waiting for IMAP Client to disconnect idling process...");
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

		private void OnIdleStopped(bool IsShutdownReqested) {
			if (IsClientInIdle) {
				StopImapIdle();
			}

			while (true) {
				if (!IsClientInIdle) {
					Logger.Log("Client imap idling has been ended sucessfully!");
					break;
				}
				else {
					Logger.Log("Waiting for client to shutdown idling connection...");
				}
			}

			List<IMessageSummary> messages = new List<IMessageSummary>();
			if (Client.Inbox.Count > InboxMessagesCount) {
				lock (Client.SyncRoot) {
					messages = Client.Inbox.Fetch(InboxMessagesCount, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).ToList();
				}

				if (messages.Count > 0 || messages != null) {
					foreach (IMessageSummary message in messages) {
						Logger.Log($"{message.Envelope.From.FirstOrDefault()}/{message.Envelope.Subject}");
					}
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
