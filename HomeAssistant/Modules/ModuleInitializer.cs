using HomeAssistant.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {
	public class ModuleInitializer {

		private Logger Logger = new Logger("MODULES");
		public DiscordClient Discord;
		public GoogleMap Map;
		public GoogleSpeech Speech;
		public Youtube Youtube;
		public ConcurrentDictionary<string, Email> EmailClientCollection = new ConcurrentDictionary<string, Email>();

		public ModuleInitializer() {
			Logger.Log("Starting modules...");
		}

		public ModuleInitializer(bool withInitilization = false) {
			if (withInitilization) {
				Task.Run(StartModules);
			}
		}

		public async Task StartModules() {
			bool DiscordLoaded = await StartDiscord().ConfigureAwait(false);
			bool EmailLoaded = StartEmail();
			Map = new GoogleMap();
			Speech = new GoogleSpeech();
			Youtube = new Youtube();

			if (DiscordLoaded && EmailLoaded) {
				Logger.Log("Sucessfully loaded all modules!");
			}
			else {
				Logger.Log("One or more modules have failed to load.", LogLevels.Warn);
			}
		}

		public async Task<bool> StartDiscord() {
			try {
				Discord = new DiscordClient();
				if (await Discord.InitDiscordClient().ConfigureAwait(false)) {
					Logger.Log("Sucessfully started discord module!");
					return true;
				}
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Error);
				return false;
			}
			return false;
		}

		public bool StartEmail() {
			if (Program.Config.EmailDetails.Count <= 0 || !Program.Config.EmailDetails.Any()) {
				Logger.Log("No email IDs found in global config. cannot start Email Module...");
				return false;
			}

			EmailClientCollection.Clear();

			int loadedCount = 0;

			foreach (KeyValuePair<string, string> entry in Program.Config.EmailDetails) {
				Email mailClient = new Email(entry.Key.Trim(), entry.Value.Trim());
				string UniqueID = mailClient.UniqueAccountID;

				if (string.IsNullOrEmpty(UniqueID) || string.IsNullOrWhiteSpace(UniqueID)) {
					UniqueID = entry.Key;
				}

				mailClient.StartImapClient(false);

				if (mailClient.AccountLoaded) {
					Logger.Log($"Sucessfully loaded {entry.Key.Trim()}");
					EmailClientCollection.TryAdd(UniqueID, mailClient);
					loadedCount++;
				}
			}

			if (loadedCount == Program.Config.EmailDetails.Count) {
				Logger.Log("Sucessfully loaded all email accounts and started IMAP Idle!");
			}
			else {
				Logger.Log($"{loadedCount} accounts loaded sucessfully, {Program.Config.EmailDetails.Count - loadedCount} account(s) failed.");
			}

			return true;
		}

		public void DisposeAllEmailClients() {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null || !EmailClientCollection.Any()) {
				return;
			}

			foreach (KeyValuePair<string, Email> pair in EmailClientCollection) {
				if (pair.Value.AccountLoaded) {
					pair.Value.DisposeClient(false);
					Logger.Log($"Disconnected {pair.Key} email account.");
				}
			}
		}

		public async Task OnCoreShutdown() {
			if (Discord.Client != null || Discord.IsServerOnline) {
				await Discord.StopServer().ConfigureAwait(false);
			}

			DisposeAllEmailClients();
			Logger.Log("Modules sucessfully shutdown!");
		}
	}
}
