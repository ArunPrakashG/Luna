using Luna.Core.Watchers.Interfaces;
using Luna.Logging;
using Luna.Logging.Interfaces;
using Luna.Modules;
using Luna.Modules.Interfaces.EventInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Luna.Modules.ModuleInitializer;

namespace Luna.Core.Watchers {
	public class GenericWatcher : IWatcher {
		private const int DELAY_SECS = 2;
		private readonly ILogger Logger = new Logger(nameof(GenericWatcher));

		private readonly FileSystemWatcher Watcher;
		private readonly Core Core;

		private readonly bool IsModuleWatcher;
		private DateTime LastRead;		

		public string FilterQuery { get; private set; }

		public string WatcherDirectory { get; private set; }

		public List<string> IgnoreList { get; private set; } = new List<string>();

		public Dictionary<string, Action<string>> Events { get; private set; } = new Dictionary<string, Action<string>>();		

		public GenericWatcher(Core _core, string _filterQuery, string _directory, bool _includeSubdirs, List<string> _ignoredFiles, Dictionary<string, Action<string>> _events) {
			if(string.IsNullOrEmpty(_filterQuery) || string.IsNullOrEmpty(_directory)) {
				throw new ArgumentNullException(nameof(_filterQuery) + "||" + nameof(_directory));
			}

			if (_events == null || _events.Count <= 0) {
				throw new ArgumentOutOfRangeException(nameof(_events));
			}

			FilterQuery = _filterQuery;
			WatcherDirectory = _directory;
			IgnoreList = _ignoredFiles;
			Events = _events;
			LastRead = DateTime.Now;
			Core = _core ?? throw new ArgumentNullException(nameof(_core));

			IsModuleWatcher = Events.Count == 1 && Events.First().Key.Equals("*");

			if (!Directory.Exists(WatcherDirectory)) {
				Logger.Warning($"The specified directory ({WatcherDirectory}) doesn't exist, Creating a new one on the path...");
				Directory.CreateDirectory(WatcherDirectory);
			}

			Watcher = new FileSystemWatcher(WatcherDirectory) {
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				Filter = FilterQuery
			};

			Watcher.Created += OnEventRaised;
			Watcher.Changed += OnEventRaised;
			Watcher.Renamed += OnEventRaised;
			Watcher.IncludeSubdirectories = _includeSubdirs;
			Watcher.EnableRaisingEvents = true;
			Logger.Trace($"File watcher has been started for path: '{WatcherDirectory}'");
		}

		public void Pause() {
			if(Watcher == null) {
				return;
			}

			Watcher.EnableRaisingEvents = false;
		}

		public void Resume() {
			if (Watcher == null) {
				return;
			}

			Watcher.EnableRaisingEvents = true;
		}		

		private void OnEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Error(nameof(sender) + " || " + nameof(e));
				return;
			}

			if (!Core.IsBaseInitiationCompleted) {
				return;
			}

			double secondsSinceLastRead = DateTime.Now.Subtract(LastRead).TotalSeconds;
			LastRead = DateTime.Now;

			if (secondsSinceLastRead <= DELAY_SECS) {
				return;
			}

			string absoluteFileName = Path.GetFileName(e.Name);

			if (string.IsNullOrEmpty(absoluteFileName) || string.IsNullOrWhiteSpace(absoluteFileName)) {
				return;
			}

			if(IgnoreList != null && IgnoreList.Contains(absoluteFileName)) {
				return;
			}

			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.WatcherEvent, new EventParameter(new object[] { sender, e }));

			if (Events.Count <= 0) {
				return;
			}

			if (IsModuleWatcher) {
				Action<string>? moduleWatcherAction = Events.FirstOrDefault().Value;

				if(moduleWatcherAction == null) {
					return;
				}

				moduleWatcherAction.Invoke(absoluteFileName);
				return;
			}

			foreach (KeyValuePair<string, Action<string>> pair in Events) {
				if (string.IsNullOrEmpty(pair.Key) || pair.Value == null) {
					continue;
				}

				if (absoluteFileName.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)) {
					Logger.Trace($"Watcher event raised '{pair.Key}'");
					pair.Value.Invoke(absoluteFileName);
					Logger.Trace("Action executed successfully!");					
				}
			}
		}

		public void StopWatcher() {
			if (Watcher == null) {
				Logger.Trace("Watcher is already stopped and disposed!");
				return;
			}

			Watcher.EnableRaisingEvents = false;
			Watcher.Dispose();
			Logger.Trace($"Stopped watcher for {WatcherDirectory} directory.");
		}
	}
}
