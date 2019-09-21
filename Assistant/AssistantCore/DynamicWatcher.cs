
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
using Assistant.Modules.Interfaces;
using Assistant.Modules.Interfaces.LoggerInterfaces;
using System;
using System.IO;

namespace Assistant.AssistantCore {

	public class DynamicWatcher : IDynamicWatcher {

		public ILoggerBase Logger { get; set; }

		public string DirectoryToWatch { get; set; }

		public int DelayBetweenReadsInSeconds { get; set; } = 2;

		public FileSystemWatcher FileSystemWatcher { get; set; }
		private DateTime LastRead = DateTime.MinValue;

		public bool WatcherOnline { get; set; } = false;

		public bool IncludeSubdirectories { get; set; } = false;

		public (bool, DynamicWatcher, FileSystemWatcher) InitWatcherService() {
			Logger.Log("Starting dynamic watcher...", Enums.LogLevels.Trace);

			if (Helpers.IsNullOrEmpty(DirectoryToWatch) || DelayBetweenReadsInSeconds <= 0 || Logger == null) {
				return (false, this, FileSystemWatcher);
			}

			if (!Directory.Exists(DirectoryToWatch)) {
				return (false, this, FileSystemWatcher);
			}

			if (FileSystemWatcher == null) {
				return (false, this, FileSystemWatcher);
			}

			FileSystemWatcher.Created += OnFileCreated;
			FileSystemWatcher.Changed += OnFileChanged;
			FileSystemWatcher.Renamed += OnFileRenamed;
			FileSystemWatcher.Deleted += OnFileDeleted;
			FileSystemWatcher.IncludeSubdirectories = IncludeSubdirectories;
			FileSystemWatcher.EnableRaisingEvents = true;
			WatcherOnline = true;
			Logger.Log($"Dynamic watcher started sucessfully! ({DirectoryToWatch})");
			return (true, this, FileSystemWatcher);
		}

		public void StopWatcherServier() {
			FileSystemWatcher.EnableRaisingEvents = false;
			WatcherOnline = false;
		}

		public void OnFileDeleted(object sender, FileSystemEventArgs e) {
		}

		public void OnFileRenamed(object sender, RenamedEventArgs e) {
		}

		public void OnFileChanged(object sender, FileSystemEventArgs e) {
		}

		public void OnFileCreated(object sender, FileSystemEventArgs e) {
		}
	}
}
