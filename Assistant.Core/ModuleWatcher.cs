using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;

namespace Assistant.Core {

	public class ModuleWatcher {
		private readonly ILogger Logger = new Logger("MODULE-WATCHER");
		private FileSystemWatcher? FileSystemWatcher;
		private DateTime LastRead = DateTime.MinValue;
		public bool ModuleWatcherOnline = false;

		public ModuleWatcher() {
			if (FileSystemWatcher != null) {
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
			}

			if (!Directory.Exists(Constants.ModuleDirectory)) {
				Directory.CreateDirectory(Constants.ModuleDirectory);
			}
		}

		public void InitModuleWatcher() {
			Logger.Log("Starting module watcher...", LogLevels.Trace);
			if (!Core.Config.EnableModuleWatcher && !Core.Config.EnableModules) {
				Logger.Log("Module watcher is disabled.", LogLevels.Trace);
				return;
			}

			FileSystemWatcher = new FileSystemWatcher(Constants.ModuleDirectory) {
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
				Filter = "*.dll"
			};

			FileSystemWatcher.Created += OnFileEventRaised;
			FileSystemWatcher.Changed += OnFileEventRaised;
			FileSystemWatcher.Renamed += OnFileEventRaised;
			FileSystemWatcher.Deleted += OnFileEventRaised;
			FileSystemWatcher.Error += FileSystemWatcherOnError;
			FileSystemWatcher.IncludeSubdirectories = true;
			FileSystemWatcher.EnableRaisingEvents = true;
			ModuleWatcherOnline = true;
			Logger.Log("Module watcher started successfully!");
		}

		private void FileSystemWatcherOnError(object sender, ErrorEventArgs e) => Logger.Log(e.GetException());

		public void StopModuleWatcher() {
			if (FileSystemWatcher != null) {
				Logger.Log("Stopping module watcher...", LogLevels.Trace);
				ModuleWatcherOnline = false;
				FileSystemWatcher.EnableRaisingEvents = false;
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
				Logger.Log("Stopped module watcher.");
			}
		}

		private void OnModuleDeleted(string filePath) {
			if (string.IsNullOrEmpty(filePath)) {
				return;
			}

			Core.ModuleLoader.UnloadFromPath(filePath);
		}

		private void OnFileEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Log(nameof(sender) + " || " + nameof(e), LogLevels.Error);
				return;
			}

			if (!Core.CoreInitiationCompleted) {
				return;
			}

			double secondsSinceLastRead = DateTime.Now.Subtract(LastRead).TotalSeconds;
			LastRead = DateTime.Now;

			if (secondsSinceLastRead <= 1) {
				return;
			}

			Logger.Log(e.FullPath, LogLevels.Trace);

			Task.Run(async () => await Core.ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.ModuleWatcherEvent, sender, e).ConfigureAwait(false));

			string fileName = e.Name;
			string absoluteFileName = Path.GetFileName(fileName);
			Logger.Log($"An event has been raised on module folder for file > {absoluteFileName}", Enums.LogLevels.Trace);


			if (e.ChangeType.Equals(WatcherChangeTypes.Deleted)) {
				OnModuleDeleted(e.FullPath);
				return;
			}

			switch (absoluteFileName) {
				case "example.dll":
					Logger.Log("Ignoring example.dll file.", Enums.LogLevels.Trace);
					break;

				default:
					Core.ModuleLoader.LoadAsync().ConfigureAwait(false);
					Core.ModuleLoader.InitServiceAsync().ConfigureAwait(false);
					break;
			}
		}
	}
}
