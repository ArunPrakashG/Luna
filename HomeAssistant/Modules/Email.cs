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
		public bool AccountLoaded = false;
		public bool CancelIdle = false;
		public bool ExitClient = false;

		public void OnModuleStarted() {

		}

		public void OnModuleShutdown() {
			DisposeClient(false);
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

		public void DisposeClient(bool withExitClient) {
			if (!CancelIdle) {
				StopImapIdle();
			}

			ExitClient = true;

			if (withExitClient) {
				Logger.Log("Waiting for IMAP Client to disconnect idling process...");

				while (true) {
					if (!Client.IsIdle) {
						if (AccountLoaded) {
							lock (Client.SyncRoot) {
								Client.Disconnect(true);
							}
						}
						break;
					}
				}

				Logger.Log("Disconnected!");
			}

			if (Client != null) {
				Client.Dispose();
			}
		}

		public void StopImapIdle() => CancelIdle = true;

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
			CancelIdle = false;
			ExitClient = false;

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
			AccountLoaded = true;

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
							if (CancelIdle) {
								break;
							}
							Task.Delay(100).Wait();
						}

						DoneToken.Cancel();
						thread.Join();

						if (ExitClient) {							
							Logger.Log("Waiting for IMAP Client to disconnect idling process...");

							while (true) {
								if (!Client.IsIdle) {
									if (AccountLoaded) {
										lock (Client.SyncRoot) {
											Client.Disconnect(true);
										}
									}
									break;
								}
								Task.Delay(100).Wait();
							}

							AccountLoaded = false;
							Logger.Log("Disconnected!");
							return;
						}

						OnIdleStopped(ExitClient);
					}
				}
				catch (Exception e) {
					Logger.Log(e, ExceptionLogLevels.Error);
					return;
				}
			}, "Idle Thread");
		}

		private void OnIdleStopped(bool exit) {
			if (Client.IsIdle) {
				StopImapIdle();
			}

			AccountLoaded = false;
			List<IMessageSummary> messages = new List<IMessageSummary>();
			if (Client.Inbox.Count > InboxMessagesCount) {

				lock (Client.SyncRoot) {
					messages = Client.Inbox.Fetch(InboxMessagesCount, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId).ToList();
				}

				if (messages.Count <= 0 || messages == null) {
					lock (Client.SyncRoot) {
						Client.Disconnect(true);
					}
					if (!exit) {
						StartImapClient(true);
					}
					return;
				}

				foreach (IMessageSummary message in messages) {
					Logger.Log($"{message.Envelope.From.FirstOrDefault()}/{message.Envelope.Subject}");
				}
			}
			lock (Client.SyncRoot) {
				Client.Disconnect(true);
			}
			if (!exit) {
				StartImapClient(true);
			}
		}

		private static void IdleLoop(object state) {
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
									idle.Client.NoOp(idle.CancellationToken);
									WaitHandle.WaitAny(new[] { timeout.Token.WaitHandle, idle.CancellationToken.WaitHandle });
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
