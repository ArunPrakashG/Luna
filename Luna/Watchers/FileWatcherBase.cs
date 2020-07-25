using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Luna.Watchers {
	internal abstract class WatcherBase {
		private DateTime LastEventTime;
		protected const int DELAY_SECS = 1;
		protected readonly InternalLogger Logger;
		protected readonly bool IsModuleWatcher;
		protected string WatcherPath;
		private FileSystemWatcher Watcher;
		private Dictionary<string, Action<string>> EventActionPairs;
		protected List<string> IgnoredFiles;

		protected WatcherBase(InternalLogger logger) {
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			IsModuleWatcher = EventActionPairs.Count == 1 && EventActionPairs.First().Key.Equals("*");
		}

		protected void Init(FileSystemWatcher fileSystemWatcher, Dictionary<string, Action<string>> events, List<string> ignoredFiles) {
			Watcher = fileSystemWatcher ?? throw new ArgumentNullException(nameof(fileSystemWatcher));
			EventActionPairs = events ?? new Dictionary<string, Action<string>>();
			IgnoredFiles = ignoredFiles ?? new List<string>();
			Watcher.Created += OnEventRaised;
			Watcher.Changed += OnEventRaised;
			Watcher.Renamed += OnEventRaised;
			WatcherPath = Watcher.Path;
			Watcher.EnableRaisingEvents = true;
		}

		protected void RegisterInternalFileEvent(Action<object, FileSystemEventArgs> eventHandler) {
			if (Watcher == null || eventHandler == null) {
				return;
			}

			Watcher.Created += (o, s) => eventHandler.Invoke(o, s);
			Watcher.Changed += (o, s) => eventHandler.Invoke(o, s);
			Watcher.Renamed += (o, s) => eventHandler.Invoke(o, s);
		}

		internal void Pause() {
			if (Watcher == null) {
				return;
			}

			Watcher.EnableRaisingEvents = false;
		}

		internal void Resume() {
			if (Watcher == null) {
				return;
			}

			Watcher.EnableRaisingEvents = true;
		}

		private void OnEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.NullError(nameof(sender) + " || " + nameof(e));
				return;
			}

			double secondsSinceLastRead = DateTime.Now.Subtract(LastEventTime).TotalSeconds;
			LastEventTime = DateTime.Now;

			if (secondsSinceLastRead <= DELAY_SECS) {
				return;
			}

			string absoluteFileName = Path.GetFileName(e.Name);

			if (string.IsNullOrEmpty(absoluteFileName)) {
				return;
			}

			if (IgnoredFiles.Where(x => x.Equals(absoluteFileName, StringComparison.OrdinalIgnoreCase)).Count() > 0) {
				return;
			}

			if (EventActionPairs.Count <= 0) {
				return;
			}

			if (IsModuleWatcher && EventActionPairs.Count >= 1) {
				EventActionPairs.FirstOrDefault().Value.Invoke(absoluteFileName);
				return;
			}

			EventActionPairs.ForEachElement((fileName, action) => {
				if (!string.IsNullOrEmpty(fileName) && action != null && absoluteFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) {
					action.Invoke(absoluteFileName);
					Logger.Trace($"Watcher event raised '{fileName}'");
				}
			});
		}

		internal void StopWatcher() {
			if (Watcher == null) {
				return;
			}

			Logger.Trace($"'{Watcher.Path}' watcher stopped.");
			Watcher.EnableRaisingEvents = false;
			Watcher.Dispose();
		}
	}
}
