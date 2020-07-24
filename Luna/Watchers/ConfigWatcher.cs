using Luna.Logging;
using Luna.Modules;
using Luna.Modules.Interfaces.EventInterfaces;
using Luna.Watchers.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Luna.Modules.ModuleInitializer;

namespace Luna.Watchers {
	internal class ConfigWatcher : FileWatcherBase {
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

		private void OnMailConfigChangeEvent(string obj) {
			
		}

		private void OnDiscordConfigChangeEvent(string obj) {
			
		}

		private void OnCoreConfigChangeEvent(string obj) {
			
		}
	}
}
