
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.IO;
using System.Threading.Tasks;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.AssistantCore {

	public class ConfigWatcher {
		private readonly Logger Logger = new Logger("CONFIG-WATCHER");
		public FileSystemWatcher FileSystemWatcher;
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

			Task.Run(async () => await Core.ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.ConfigWatcherEvent, sender, e).ConfigureAwait(false));

			switch (absoluteFileName) {
				case "Assistant.json":
					Logger.Log("Config watcher event raised for core config file.", Enums.LogLevels.Trace);

					if (e.ChangeType == WatcherChangeTypes.Deleted) {
						Logger.Log("The core config file has been deleted.", Enums.LogLevels.Warn);
						Logger.Log("Fore quitting assistant.", Enums.LogLevels.Warn);
						Task.Run(async () => await Core.Exit(0).ConfigureAwait(false));
					}

					Logger.Log("Updating core config as the local config file as been updated...");
					Core.Config = Core.Config.LoadConfig();
					break;

				case "GpioConfig.json":
					Logger.Log("Config watcher event raised for GPIO Config file.", Enums.LogLevels.Trace);

					if (e.ChangeType == WatcherChangeTypes.Deleted) {
						Logger.Log("The Gpio config file has been deleted.", Enums.LogLevels.Warn);
						Logger.Log("Fore quitting assistant.", Enums.LogLevels.Warn);
						Task.Run(async () => await Core.Exit(0).ConfigureAwait(false));
					}

					Logger.Log("Updating gpio config as the local config as been updated...");
					Core.Controller.GpioConfigCollection = Core.GPIOConfigHandler.LoadConfig().GPIOData;
					break;

				case "MailConfig.json":
					Logger.Log("Mail config has been modified.", Enums.LogLevels.Trace);
					//TODO: handle MailConfig file change events
					break;

				case "DiscordBot.json":
					Logger.Log("Discord bot config has been modified.", Enums.LogLevels.Trace);
					//TODO: handle DiscordBot config file change events
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
