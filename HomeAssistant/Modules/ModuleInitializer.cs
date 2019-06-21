using HomeAssistant.Core;
using HomeAssistant.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAssistant.Extensions;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");
		public DiscordClient Discord { get; set; }
		public GoogleMap Map { get; set; }
		public Youtube Youtube { get; set; }
		public Email Mail { get; set; }

		public ConcurrentDictionary<string, EmailBot> EmailClientCollection { get; set; } = new ConcurrentDictionary<string, EmailBot>();

		public async Task<(DiscordClient, Email, GoogleMap, Youtube)> StartModules() {
			await StartDiscord().ConfigureAwait(false);
			Helpers.InBackground(StartEmail);
			Map = new GoogleMap();
			Youtube = new Youtube();
			return (Discord, Mail, Map, Youtube);
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
				Logger.Log(e, LogLevels.Error);
				return false;
			}
			return false;
		}

		private bool StartEmail() {
			if (Tess.Config.EmailDetails.Count <= 0 || !Tess.Config.EmailDetails.Any()) {
				Logger.Log("No email IDs found in global config. cannot start Email Module...", LogLevels.Trace);
				return false;
			}

			EmailClientCollection.Clear();
			Mail = new Email();

			int loadedCount = 0;
			foreach (KeyValuePair<string, EmailConfig> entry in Tess.Config.EmailDetails) {
				if (string.IsNullOrEmpty(entry.Value.EmailID) || string.IsNullOrWhiteSpace(entry.Value.EmailPASS)) {
					continue;
				}

				string uniqueId = entry.Key;

				try {
					(bool result, EmailBot emailBot) = Mail.InitBot(uniqueId, entry.Value);

					if (result) {
						loadedCount++;
					}
					else {
						Logger.Log($"Failed to load {entry.Value.EmailID} account.", LogLevels.Trace);
					}
				}
				catch (NullReferenceException) {
					Logger.Log($"Failed to load {entry.Value.EmailID} account.", LogLevels.Trace);
					continue;
				}
			}

			if (Tess.Config.EmailDetails.Count - loadedCount > 0) {
				Logger.Log($"{Tess.Config.EmailDetails.Count - loadedCount} account(s) failed to load.", LogLevels.Warn);
			}
			
			Logger.Log($"{loadedCount} accounts loaded successfully.",LogLevels.Trace);
			return true;
		}

		public void DisposeEmailBot(string botUniqueId) {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null) {
				return;
			}

			foreach (KeyValuePair<string, EmailBot> pair in EmailClientCollection) {
				if (pair.Key.Equals(botUniqueId)) {
					pair.Value.Dispose();
					Logger.Log($"Disposed {pair.Value.GmailId} email account.");
				}
			}
		}

		public void DisposeAllEmailClients() {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null) {
				return;
			}

			foreach (KeyValuePair<string, EmailBot> pair in EmailClientCollection) {
				if (pair.Value.IsAccountLoaded) {
					pair.Value.Dispose();
					Logger.Log($"Disposed {pair.Value.GmailId} email account.");
				}
			}
			EmailClientCollection.Clear();
		}

		public bool OnCoreShutdown() {
			if (Discord != null && (Discord.Client != null || Discord.IsServerOnline)) {
				Logger.Log("Discord server shutting down...", LogLevels.Trace);
				_ = Discord.StopServer().Result;
			}

			if (EmailClientCollection.Count > 0 && EmailClientCollection != null) {
				Logger.Log("Email clients shutting down...", LogLevels.Trace);
				DisposeAllEmailClients();
			}

			Logger.Log("Module shutdown sucessfull.");
			return true;
		}
	}
}
