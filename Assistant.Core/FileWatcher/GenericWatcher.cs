using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using static Assistant.Modules.ModuleInitializer;

namespace Assistant.Core.FileWatcher {
	public class GenericWatcher : IWatcher {
		public ILogger Logger { get; set; } = new Logger("FILE-WATCHER");
		private const int DELAY_SECS = 2;

		public bool IsOnline {
			get => Watcher != null && Watcher.EnableRaisingEvents; set {
				if (Watcher != null) {
					Watcher.EnableRaisingEvents = false;
				}
			}
		}

		public string? WatcherFilter { get; set; }
		public string? WatcherDirectory { get; set; }
		public List<string> IgnoreList { get; set; } = new List<string>();
		public Dictionary<string, Action> WatcherFileEvents { get; set; } = new Dictionary<string, Action>();
		public FileSystemWatcher? Watcher { get; set; }
		public DateTime LastRead { get; set; }

		public void InitWatcher(string? dir, Dictionary<string, Action> watcherFileEvents, List<string> ignoreList, string? filter = "*.json", bool includeSubs = false) {
			if (string.IsNullOrEmpty(filter) || string.IsNullOrEmpty(dir)) {
				Logger.Warning("Directory or filter of the watcher isn't specified or is invalid.");
				return;
			}

			if (!Directory.Exists(dir)) {
				Logger.Warning($"The specified directory ({dir}) doesn't exist, Creating a new one on the path...");
				Directory.CreateDirectory(dir);
			}

			if (watcherFileEvents == null || watcherFileEvents.Count <= 0) {
				Logger.Warning("File events can't be null or empty.");
				return;
			}

			WatcherFilter = filter;
			WatcherDirectory = dir;
			IgnoreList = ignoreList;
			WatcherFileEvents = watcherFileEvents;
			LastRead = DateTime.Now;

			Watcher = new FileSystemWatcher(WatcherDirectory) {
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				Filter = WatcherFilter
			};

			//TODO: Check for duplicates in the dictionary

			Watcher.Created += OnFileEventRaised;
			Watcher.Changed += OnFileEventRaised;
			Watcher.Renamed += OnFileEventRaised;
			Watcher.IncludeSubdirectories = includeSubs;
			Watcher.EnableRaisingEvents = true;
			Logger.Log("Watcher started successfully!");
		}

		private void OnFileEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Error(nameof(sender) + " || " + nameof(e));
				return;
			}

			if (!Core.CoreInitiationCompleted) {
				return;
			}

			double secondsSinceLastRead = DateTime.Now.Subtract(LastRead).TotalSeconds;
			LastRead = DateTime.Now;

			if (secondsSinceLastRead <= DELAY_SECS) {
				return;
			}

			string fileName = e.Name;
			string absoluteFileName = Path.GetFileName(fileName);

			if (string.IsNullOrEmpty(absoluteFileName) || string.IsNullOrWhiteSpace(absoluteFileName)) {
				return;
			}

			Helpers.InBackground(async () => await Core.ModuleLoader.ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT.WatcherEvent, sender, e).ConfigureAwait(false));

			foreach (KeyValuePair<string, Action> pair in WatcherFileEvents) {
				if (string.IsNullOrEmpty(pair.Key) || pair.Value == null) {
					continue;
				}

				if (absoluteFileName.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)) {
					Logger.Trace($"Watcher file event raised for -> {pair.Key}; Executing corresponding action...");
					pair.Value.Invoke();
					break;
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
