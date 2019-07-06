using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.IO;

namespace HomeAssistant.AssistantCore {
	public class ModuleWatcher {
		private readonly Logger Logger = new Logger("MODULE-WATCHER");
		private FileSystemWatcher FileSystemWatcher;
		private DateTime LastRead = DateTime.MinValue;
		public bool ModuleWatcherOnline = false;

		public ModuleWatcher() {
			if (FileSystemWatcher != null){
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
			}

			if (!Directory.Exists(Constants.ModuleDirectory)) {
				Directory.CreateDirectory(Constants.ModuleDirectory);
			}
		}

		public void InitConfigWatcher() {
			Logger.Log("Starting module watcher...", Enums.LogLevels.Trace);
			if (!Core.Config.EnableModuleWatcher && !Core.Config.EnableModules) {
				Logger.Log("Module watcher is disabled.", Enums.LogLevels.Trace);
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
			Logger.Log("Module watcher started sucessfully!");
		}

		private void FileSystemWatcherOnError(object sender, ErrorEventArgs e) {
			Logger.Log(e.GetException());
		}

		public void StopModuleWatcher() {
			if (FileSystemWatcher != null) {
				Logger.Log("Stopping module watcher...", Enums.LogLevels.Trace);
				ModuleWatcherOnline = false;
				FileSystemWatcher.EnableRaisingEvents = false;
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
				Logger.Log("Stopped module watcher sucessfully.");
			}
		}

		private void OnModuleDeleted (string fileName, string filePath, Enums.ModuleLoaderContext context) {
			if (Helpers.IsNullOrEmpty(fileName) || Helpers.IsNullOrEmpty(filePath)) {
				return;
			}

			Core.ModuleLoader.UnloadModules(context);
		}

		private void OnFileEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Log(nameof(sender) + " || " + nameof(e), Enums.LogLevels.Error);
				return;
			}

			if (!Core.CoreInitiationCompleted) { return; }

			double secondsSinceLastRead = DateTime.Now.Subtract(LastRead).TotalSeconds;
			LastRead = DateTime.Now;
			
			if (secondsSinceLastRead <= 1) {
				return;
			}

			Logger.Log(e.FullPath, Enums.LogLevels.Trace);
			string t = new DirectoryInfo(e.FullPath).Parent.Name;
			Logger.Log(t, Enums.LogLevels.Trace);

			Enums.ModuleLoaderContext loaderContext;
			if (t.Equals("DiscordModules", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.DiscordClients;
			}
			else if (t.Equals("EmailClients", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.EmailClients;
			}
			else if (t.Equals("GoogleMapModules", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.GoogleMaps;
			}
			else if (t.Equals("MiscModules", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.MiscModules;
			}
			else if (t.Equals("SteamClients", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.SteamClients;
			}
			else if (t.Equals("YoutubeClients", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.YoutubeClients;
			}
			else if (t.Equals("Loggers", StringComparison.OrdinalIgnoreCase)) {
				loaderContext = Enums.ModuleLoaderContext.Logger;
			}
			else {
				loaderContext = Enums.ModuleLoaderContext.None;
			}

			string fileName = e.Name;
			string absoluteFileName = Path.GetFileName(fileName);
			Logger.Log($"An event has been raised on module folder for file > {absoluteFileName}", Enums.LogLevels.Trace);
			if (string.IsNullOrEmpty(absoluteFileName) || string.IsNullOrWhiteSpace(absoluteFileName)) {
				return;
			}

			if (e.ChangeType.Equals(WatcherChangeTypes.Deleted)) {
				OnModuleDeleted(e.Name, e.FullPath, loaderContext);
				return;
			}

			switch (absoluteFileName) {
				case "example.dll":
					Logger.Log("Ignoring example.dll file.", Enums.LogLevels.Trace);
					break;
				default:
					Helpers.InBackground(() => {
						Logger.Log("Loading dll...", Enums.LogLevels.Trace);
						(bool, Modules.LoadedModules) status = Core.ModuleLoader.LoadModules(loaderContext);
					});
					break;
			}
		}
	}
}
