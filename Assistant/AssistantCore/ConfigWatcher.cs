using System;
using System.IO;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.AssistantCore {

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
			Logger.Log("Starting config watcher...", Enums.LogLevels.Trace);
			if (!Core.Config.EnableConfigWatcher) {
				Logger.Log("config watcher is disabled.", Enums.LogLevels.Trace);
				return;
			}

			if (!File.Exists(Constants.CoreConfigPath)) {
				Logger.Log("Core config directory doesn't exist. cannot start config watcher.", Enums.LogLevels.Error);
				return;
			}

			FileSystemWatcher = new FileSystemWatcher(Constants.ConfigDirectory) {
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				Filter = "*.json"
			};

			FileSystemWatcher.Created += OnFileEventRaised;
			FileSystemWatcher.Changed += OnFileEventRaised;
			FileSystemWatcher.Renamed += OnFileEventRaised;
			FileSystemWatcher.IncludeSubdirectories = false;
			FileSystemWatcher.EnableRaisingEvents = true;
			ConfigWatcherOnline = true;
			Logger.Log("Config watcher started sucessfully!");
		}

		public void StopConfigWatcher() {
			if (FileSystemWatcher != null) {
				Logger.Log("Stopping config watcher...", Enums.LogLevels.Trace);
				ConfigWatcherOnline = false;
				FileSystemWatcher.EnableRaisingEvents = false;
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
				Logger.Log("Stopped config watcher sucessfully.");
			}
		}

		private void OnFileEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Log(nameof(sender) + " || " + nameof(e), Enums.LogLevels.Error);
				return;
			}

			if (!Core.CoreInitiationCompleted) { return; }

			double secondsSinceLastRead = DateTime.Now.Subtract(LastRead).TotalSeconds;
			LastRead = DateTime.Now;

			if (secondsSinceLastRead <= 2) {
				return;
			}

			string fileName = e.Name;
			string absoluteFileName = Path.GetFileName(fileName);

			if (string.IsNullOrEmpty(absoluteFileName) || string.IsNullOrWhiteSpace(absoluteFileName)) {
				return;
			}

			switch (absoluteFileName) {
				case "Assistant.json":
					Logger.Log("Config watcher event raised for core config file.", Enums.LogLevels.Trace);
					Logger.Log("Updating core config as the local config file as been updated...");
					Core.Config = Core.Config.LoadConfig(true);
					break;

				case "GpioConfig.json":
					Logger.Log("Config watcher event raised for GPIO Config file.", Enums.LogLevels.Trace);
					Logger.Log("Updating gpio config as the local config as been updated...");
					Core.Controller.GpioConfigCollection = Core.GPIOConfigHandler.LoadConfig().GPIOData;
					break;

				case "MailConfig.json":
					Logger.Log("Mail config has been modified.", Enums.LogLevels.Trace);
					break;

				case "DiscordBot.json":
					Logger.Log("Discord bot config has been modified.", Enums.LogLevels.Trace);
					break;

				case "AssistantExample.json":
				case "GpioConfigExample.json":
					Logger.Log("File watcher event raised for example configs, ignored.", Enums.LogLevels.Trace);
					break;

				default:
					Logger.Log($"File watcher event raised for unknown file. ({absoluteFileName}) ignored.", Enums.LogLevels.Trace);
					break;
			}
		}
	}
}
