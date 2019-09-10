
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

using Assistant.Extensions;
using Assistant.Log;
using Assistant.Modules.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.AssistantCore {

	public class ModuleWatcher {
		private readonly Logger Logger = new Logger("MODULE-WATCHER");
		private FileSystemWatcher FileSystemWatcher;
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

		private void FileSystemWatcherOnError(object sender, ErrorEventArgs e) => Logger.Log(e.GetException());

		public void StopModuleWatcher() {
			if (FileSystemWatcher != null) {
				Logger.Log("Stopping module watcher...", Enums.LogLevels.Trace);
				ModuleWatcherOnline = false;
				FileSystemWatcher.EnableRaisingEvents = false;
				FileSystemWatcher.Dispose();
				FileSystemWatcher = null;
				Logger.Log("Stopped module watcher.");
			}
		}

		private void OnModuleDeleted(string filePath) {
			if (Helpers.IsNullOrEmpty(filePath)) {
				return;
			}
		
			Core.ModuleLoader.UnloadFromPath(filePath);
		}

		private void OnFileEventRaised(object sender, FileSystemEventArgs e) {
			if ((sender == null) || (e == null)) {
				Logger.Log(nameof(sender) + " || " + nameof(e), Enums.LogLevels.Error);
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

			Logger.Log(e.FullPath, Enums.LogLevels.Trace);			

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
					Core.ModuleLoader.LoadAndStartModulesOfType<IDiscordBot>(true);
					Core.ModuleLoader.LoadAndStartModulesOfType<IEmailClient>(true);
					Core.ModuleLoader.LoadAndStartModulesOfType<IYoutubeClient>(true);
					Core.ModuleLoader.LoadAndStartModulesOfType<ISteamClient>(true);
					Core.ModuleLoader.LoadAndStartModulesOfType<IAsyncEventBase>(true);
					break;
			}
		}
	}
}
