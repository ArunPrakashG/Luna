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
		private readonly Logger Logger = new Logger("MODULES");
		public DiscordClient Discord;
		public GoogleMap Map;
		public Youtube Youtube;
		public Email Mail;
		public ConcurrentDictionary<string, EmailBot> EmailClientCollection = new ConcurrentDictionary<string, EmailBot>();

		public async Task StartModules() {
			await StartDiscord().ConfigureAwait(false);
			StartEmail();
			Map = new GoogleMap();
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
				Logger.Log(e, LogLevels.Error);
				return false;
			}
			return false;
		}

		private bool StartEmail() {
			if (!Tess.Config.Debug) {
				Logger.Log("Disabled for now until email.cs bug fix.", LogLevels.Warn);
				Logger.Log("Enable debug mode to start.", LogLevels.Warn);
				return false;
			}

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

				(bool result, EmailBot emailBot) = Mail.InitBot(uniqueId, entry.Value);

				if (result) {
					EmailClientCollection.TryAdd(uniqueId, emailBot);
					loadedCount++;
				}
				else {
					Logger.Log($"Failed to load {uniqueId} account.", LogLevels.Trace);
				}
			}

			Logger.Log(
				loadedCount == Tess.Config.EmailDetails.Count
					? "Successfully loaded all email accounts!"
					: $"{loadedCount} accounts loaded successfully, {Tess.Config.EmailDetails.Count - loadedCount} account(s) failed.",
				LogLevels.Trace);

			return true;
		}

		public void DisposeEmailBot(string botUniqueId) {
			if (EmailClientCollection.Count <= 0 || EmailClientCollection == null) {
				return;
			}

			foreach (KeyValuePair<string, EmailBot> pair in EmailClientCollection) {
				if (pair.Key.Equals(botUniqueId)) {
					pair.Value.Dispose();
					Logger.Log($"Disposed {botUniqueId} email account.", LogLevels.Trace);
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
					Logger.Log($"Disposed {pair.Key} email account successfully!", LogLevels.Trace);
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

			Logger.Log("Modules successfully shutdown!");
			return true;
		}
	}
}
