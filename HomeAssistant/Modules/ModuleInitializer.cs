using HomeAssistant.Core;
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

		public async Task StartModules() {
			await StartDiscord().ConfigureAwait(false);
			StartEmail();
			Map = new GoogleMap();
			Speech = new GoogleSpeech();
			Youtube = new Youtube();
		}

		private async Task<bool> StartDiscord() {
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

		private bool StartEmail() {
			if (Program.Config.EmailDetails.Count <= 0 || !Program.Config.EmailDetails.Any()) {
				Logger.Log("No email IDs found in global config. cannot start Email Module...", LogLevels.Trace);
				return false;
			}

			EmailClientCollection.Clear();

			int loadedCount = 0;

			foreach (KeyValuePair<string, EmailConfig> entry in Program.Config.EmailDetails) {
				if (string.IsNullOrEmpty(entry.Value.EmailID) || string.IsNullOrWhiteSpace(entry.Value.EmailPASS)) {
					continue;
				}

				string UniqueID = entry.Key;
				Email mailClient = new Email(UniqueID, entry.Value);

				mailClient.StartImapClient(false);

				if (mailClient.IsAccountLoaded) {
					Logger.Log($"Sucessfully loaded {entry.Key.Trim()}", LogLevels.Trace);
					EmailClientCollection.TryAdd(UniqueID, mailClient);
					loadedCount++;
				}
			}

			if (loadedCount == Program.Config.EmailDetails.Count) {
				Logger.Log("Sucessfully loaded all email accounts and started IMAP Idle!", LogLevels.Trace);
			}
			else {
				Logger.Log($"{loadedCount} accounts loaded sucessfully, {Program.Config.EmailDetails.Count - loadedCount} account(s) failed.", LogLevels.Trace);
			}

			return true;
		}

		public void DisposeAllEmailClients() {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null || !EmailClientCollection.Any()) {
				return;
			}

			foreach (KeyValuePair<string, Email> pair in EmailClientCollection) {
				if (pair.Value.IsAccountLoaded) {
					pair.Value.DisposeClient();
					Logger.Log($"Disconnected {pair.Key} email account sucessfully!", LogLevels.Trace);
					EmailClientCollection.TryRemove(pair.Key, out _);
				}
			}
			EmailClientCollection.Clear();
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
