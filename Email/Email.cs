
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

using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Modules.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Email {
	public class Email : IModuleBase, IEmailClient {
		private readonly Logger Logger = new Logger("EMAIL-HANDLER");

		public bool RequiresInternetConnection { get; set; }

		public string ModuleIdentifier { get; set; }

		public int ModuleType { get; set; } = 1;
		public string ModulePath { get; set; } = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), nameof(Email) + ".dll");

		public Version ModuleVersion { get; set; } = new Version("5.0.0.0");

		public string ModuleAuthor { get; set; } = "Arun Prakash";

		public static EmailConfigRoot ConfigRoot { get; set; }

		public ConcurrentDictionary<string, IEmailBot> EmailClientCollection { get; set; } = new ConcurrentDictionary<string, IEmailBot>();

		public (bool, ConcurrentDictionary<string, IEmailBot>) InitEmailBots() {
			ConfigRoot = LoadConfig();

			if (ConfigRoot == null) {
				return (false, null);
			}

			if (!ConfigRoot.EnableEmailModule) {
				Logger.Log("Email module is disabled in config file.", LogLevels.Info);
				return (true, new ConcurrentDictionary<string, IEmailBot>());
			}
			if (ConfigRoot.EmailDetails.Count <= 0 || !ConfigRoot.EmailDetails.Any()) {
				Logger.Log("No email IDs found in global config. cannot start Email Module...", LogLevels.Trace);
				return (true, new ConcurrentDictionary<string, IEmailBot>());
			}

			EmailClientCollection.Clear();
			int loadedAccounts = 0;

			try {
				foreach (KeyValuePair<string, EmailConfig> entry in ConfigRoot.EmailDetails) {
					if (string.IsNullOrEmpty(entry.Value.EmailID) || string.IsNullOrWhiteSpace(entry.Value.EmailPass)) {
						continue;
					}

					string uniqueId = entry.Key;
					int connectionTry = 1;
					int maxConnectionRetry = 5;

					while (true) {
						if (connectionTry > maxConnectionRetry) {
							Logger.Log(
								$"Connection to gmail account {entry.Value.EmailID} failed after {maxConnectionRetry} attempts. cannot proceed for this account.",
								LogLevels.Error);
							return (false, null);
						}

						try {
							Logger BotLogger = new Logger(entry.Value.EmailID?.Split('@')?.FirstOrDefault()?.Trim());
							Logger.Log($"Loaded {entry.Value.EmailID} mail account to processing state.", LogLevels.Trace);

							ImapClient BotClient = Core.Config.Debug
								? new ImapClient(new ProtocolLogger(uniqueId + ".txt")) {
									ServerCertificateValidationCallback =
										(sender, certificate, chain, sslPolicyErrors) => true
								}
								: new ImapClient() {
									ServerCertificateValidationCallback =
										(sender, certificate, chain, sslPolicyErrors) => true
								};

							BotClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
							BotClient.Authenticate(entry.Value.EmailID, entry.Value.EmailPass);
							BotClient.Inbox.Open(FolderAccess.ReadWrite);

							if (BotClient.IsConnected && BotClient.IsAuthenticated) {
								Logger.Log($"Connected and authenticated IDLE-CLIENT for {entry.Value.EmailID}", LogLevels.Trace);
							}

							Task.Delay(500).Wait();

							ImapClient BotHelperClient = new ImapClient {
								ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
							};

							BotHelperClient.Connect(Constants.GmailHost, Constants.GmailPort, true);
							BotHelperClient.Authenticate(entry.Value.EmailID, entry.Value.EmailPass);
							BotHelperClient.Inbox.Open(FolderAccess.ReadWrite);

							if (BotHelperClient.IsConnected && BotHelperClient.IsAuthenticated) {
								Logger.Log($"Connected and authenticated HELPER-CLIENT for {entry.Value.EmailID}", LogLevels.Trace);
							}

							EmailBot Bot = new EmailBot();
							(bool, IEmailBot) registerResult =
								Bot.RegisterBot(BotLogger, this, entry.Value, BotClient, BotHelperClient, uniqueId).Result;

							if (registerResult.Item1) {
								AddBotToCollection(uniqueId, Bot);
							}

							if ((registerResult.Item2.IsAccountLoaded || registerResult.Item1) &&
								EmailClientCollection.ContainsKey(uniqueId)) {
								Logger.Log($"Connected and authenticated {registerResult.Item2.GmailId} account!");
								loadedAccounts++;
								break;
							}
							else {
								connectionTry++;
								continue;
							}
						}
						catch (AuthenticationException) {
							Logger.Log(
								$"Account password must be incorrect. we will retry to connect. ({connectionTry++}/{maxConnectionRetry})");
						}
						catch (SocketException) {
							Logger.Log(
								$"Network connectivity problem occured, we will retry to connect. ({connectionTry++}/{maxConnectionRetry})",
								LogLevels.Warn);
						}
						catch (OperationCanceledException) {
							Logger.Log(
								$"An operation has been cancelled, we will retry to connect. ({connectionTry++}/{maxConnectionRetry})",
								LogLevels.Warn);
						}
						catch (IOException) {
							Logger.Log(
								$"IO exception occured. we will retry to connect. ({connectionTry++}/{maxConnectionRetry})",
								LogLevels.Warn);
						}
						catch (Exception e) {
							Logger.Log(e, LogLevels.Error);
						}
					}
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return (false, EmailClientCollection);
			}

			Logger.Log($"Loaded {loadedAccounts} accounts out of {ConfigRoot.EmailDetails.Count} accounts in config.", LogLevels.Trace);
			return (true, EmailClientCollection);
		}

		public void DisposeEmailBot(string botUniqueId) {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null) {
				return;
			}

			foreach (KeyValuePair<string, IEmailBot> pair in EmailClientCollection) {
				if (pair.Key.Equals(botUniqueId)) {
					pair.Value.Dispose(false);
					Logger.Log($"Disposed {pair.Value.GmailId} email account.");
				}
			}
		}

		public bool DisposeAllEmailBots() {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null) {
				return false;
			}

			foreach (KeyValuePair<string, IEmailBot> pair in EmailClientCollection) {
				if (pair.Value.IsAccountLoaded) {
					pair.Value.Dispose(Core.GracefullModuleShutdown);
					Logger.Log($"Disposed {pair.Value.GmailId} email account.");
				}
			}
			EmailClientCollection.Clear();
			return true;
		}

		public void AddBotToCollection(string uniqueId, IEmailBot bot) {
			if (EmailClientCollection.ContainsKey(uniqueId)) {
				Logger.Log("Deleting duplicate entry of current instance.", LogLevels.Trace);
				EmailClientCollection.TryRemove(uniqueId, out _);
			}

			EmailClientCollection.TryAdd(uniqueId, bot);
			Logger.Log("Added current instance to client collection.", LogLevels.Trace);
		}

		private EmailConfigRoot LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			string JSON;
			string EmailConfigPath = Constants.ConfigDirectory + "/MailConfig.json";
			using (FileStream Stream = new FileStream(EmailConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			EmailConfigRoot returnConfig = JsonConvert.DeserializeObject<EmailConfigRoot>(JSON);
			Logger.Log("Email Configuration Loaded Successfully!", LogLevels.Trace);
			return returnConfig;
		}

		private EmailConfigRoot SaveConfig(EmailConfigRoot config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			string EmailConfigPath = Constants.ConfigDirectory + "/MailConfig.json";

			if (!File.Exists(EmailConfigPath)) {
				return null;
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(config, Formatting.Indented);
			using (StreamWriter sw = new StreamWriter(EmailConfigPath, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, config);
					Logger.Log("Updated Email Config!", LogLevels.Trace);
					sw.Dispose();
					return config;
				}
			}
		}

		public void RemoveBotFromCollection(string uniqueId) {
			if (EmailClientCollection.ContainsKey(uniqueId)) {
				Logger.Log("Remove current bot instance from collection.", LogLevels.Trace);
				EmailClientCollection.TryRemove(uniqueId, out _);
			}
		}

		public bool InitModuleService() {
			RequiresInternetConnection = true;			
			(bool, ConcurrentDictionary<string, IEmailBot>) result = InitEmailBots();
			if (result.Item1) {
				return true;
			}

			return false;
		}

		public bool InitModuleShutdown() {
			SaveConfig(ConfigRoot);
			return DisposeAllEmailBots();
		}
	}
}
