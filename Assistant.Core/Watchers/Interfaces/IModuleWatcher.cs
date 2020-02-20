using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace Assistant.Core.Watchers.Interfaces
{
	public interface IModuleWatcher
	{
		ILogger Logger { get; set; }

		bool IsOnline { get; set; }

		string? WatcherFilter { get; set; }

		List<Action<string>> WatcherEvents { get; set; }

		string? WatcherDirectory { get; set; }

		List<string> IgnoreList { get; set; }

		FileSystemWatcher? Watcher { get; set; }

		DateTime LastRead { get; set; }

		void InitWatcher(string? dir, List<Action<string>> watcherFileEvents, List<string> ignoreList, string? filter = "*.dll", bool includeSubs = false);

		void StopWatcher();
	}
}
