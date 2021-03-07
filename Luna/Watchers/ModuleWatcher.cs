using Luna.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Luna.Watchers {
	internal class ModuleWatcher : WatcherBase {
		private readonly Core Core;

		internal ModuleWatcher(Core core) : base(new InternalLogger(nameof(ConfigWatcher))) {
			Core = core ?? throw new ArgumentNullException(nameof(core));

			Dictionary<string, Action<string>> events = new Dictionary<string, Action<string>>(1) {
				{ "*", OnModuleDirectoryChangeEvent }
			};

			List<string> ignoredFiles = new List<string>();
			FileSystemWatcher watcher = new FileSystemWatcher(Constants.ModuleDirectory) {
				IncludeSubdirectories = false,
				Filter = "*.dll",
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
			};

			this.Init(watcher, events, ignoredFiles);
			Logger.Trace($"Watcher has been started for path: '{WatcherPath}'");
		}

		private void OnModuleDirectoryChangeEvent(string obj) {

		}
	}
}
