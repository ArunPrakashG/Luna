using Luna.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Luna.Watchers {
	internal class ConfigWatcher : WatcherBase {
		private readonly Core Core;

		internal ConfigWatcher(Core core) : base(new InternalLogger(nameof(ConfigWatcher))) {
			Core = core ?? throw new ArgumentNullException(nameof(core));

			Dictionary<string, Action<string>> events = new Dictionary<string, Action<string>>(3) {
				{ "Assistant.json", OnCoreConfigChangeEvent },
				{ "DiscordBot.json", OnDiscordConfigChangeEvent },
				{ "MailConfig.json", OnMailConfigChangeEvent }
			};

			List<string> ignoredFiles = new List<string>();
			FileSystemWatcher watcher = new FileSystemWatcher(Constants.ConfigDirectory) {
				IncludeSubdirectories = false,
				Filter = "*.json",
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
			};

			this.Init(watcher, events, ignoredFiles);
			Logger.Trace($"Watcher has been started for path: '{WatcherPath}'");
		}

		private void OnMailConfigChangeEvent(string fileName) {
			Logger.Info($"Change detected -> {Path.GetFileNameWithoutExtension(fileName)}");
		}

		private void OnDiscordConfigChangeEvent(string fileName) {
			Logger.Info($"Change detected -> {Path.GetFileNameWithoutExtension(fileName)}");
		}

		private async void OnCoreConfigChangeEvent(string fileName) {
			Logger.Info($"Change detected -> {Path.GetFileNameWithoutExtension(fileName)}");
			await Core.GetCoreConfig().LoadAsync().ConfigureAwait(false);
		}
	}
}
