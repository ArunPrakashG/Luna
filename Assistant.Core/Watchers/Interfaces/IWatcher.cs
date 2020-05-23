using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace Assistant.Core.Watchers.Interfaces {
	public interface IWatcher {
		string FilterQuery { get; }

		Dictionary<string, Action<string>> Events { get; }

		string WatcherDirectory { get; }

		List<string> IgnoreList { get; }

		void Pause();

		void Resume();

		void StopWatcher();
	}
}
