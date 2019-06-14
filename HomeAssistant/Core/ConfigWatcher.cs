using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.IO;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Core {
	public class ConfigWatcher {
		private readonly Logger Logger = new Logger("CONFIG-WATCHER");
		private FileSystemWatcher FileSystemWatcher;
		private DateTime LastRead = DateTime.MinValue;
		public bool ConfigWatcherOnline = false;

		public ConfigWatcher() {
			if (FileSystemWatcher != null) {
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
			}

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}
		}

		public void InitConfigWatcher() {
			Logger.Log("Starting config watcher...", LogLevels.Trace);
			if (!Tess.Config.EnableConfigWatcher) {
				Logger.Log("config watcher is disabled.", LogLevels.Trace);
				return;
			}

			if (!File.Exists(Constants.CoreConfigPath)) {
				Logger.Log("Core config file doesn't exist. cannot start file system watcher.", LogLevels.Error);
				return;
			}

			FileSystemWatcher = new FileSystemWatcher(Constants.ConfigDirectory) {
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.CreationTime,
				Filter = "*.json"
			};

			FileSystemWatcher.Created += OnFileEventRaised;
			FileSystemWatcher.Changed += OnFileEventRaised;
			FileSystemWatcher.Renamed += OnFileEventRaised;
			FileSystemWatcher.EnableRaisingEvents = true;
			ConfigWatcherOnline = true;
			Logger.Log("Config watcher started sucessfully!");
		}

		public void StopConfigWatcher() {
			if (FileSystemWatcher != null) {
				Logger.Log("Stopping config watcher...", LogLevels.Trace);
				ConfigWatcherOnline = false;
				FileSystemWatcher.EnableRaisingEvents = false;
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
				Logger.Log("Stopped config watcher sucessfully.");
			}
		}

		private void OnFileEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Log(nameof(sender) + " || " + nameof(e), LogLevels.Error);
				return;
			}

			if (!Tess.CoreInitiationCompleted) { return; }

			double secondsSinceLastRead = DateTime.Now.Subtract(LastRead).TotalSeconds;
			LastRead = DateTime.Now;

			if (secondsSinceLastRead <= 10) {
				return;
			}

			string fileName = e.Name;
			string absoluteFileName = Path.GetFileName(fileName);

			if (string.IsNullOrEmpty(absoluteFileName) || string.IsNullOrWhiteSpace(absoluteFileName)) {
				return;
			}

			switch (absoluteFileName) {
				case "TESS.json":
					Logger.Log("Config watcher event raised for core config file.", LogLevels.Trace);
					Logger.Log("Updating core config as the local config file as been updated...");
					Helpers.InBackground(() => Tess.Config = Tess.Config.LoadConfig(true));
					break;
				case "GPIOConfig.json":
					Logger.Log("Config watcher event raised for GPIO Config file.", LogLevels.Trace);
					Logger.Log("Updating gpio config as the local config as been updated...");
					Helpers.InBackground(() => Tess.Controller.GPIOConfig = Tess.GPIOConfigHandler.LoadConfig().GPIOData);
					break;
				case "TESS_EXAMPLE.json":
				case "GPIOConfig_EXAMPLE.json":
					Logger.Log("File watcher event raised for example configs, ignored.", LogLevels.Trace);
					break;
				default:
					Logger.Log($"File watcher event raised for unknown file. ({absoluteFileName}) ignored.", LogLevels.Trace);
					break;
			}
		}
	}
}
