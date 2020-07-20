using Luna.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace Luna.Watchers.Interfaces {
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
