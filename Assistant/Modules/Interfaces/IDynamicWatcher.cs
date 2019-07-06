using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assistant.Core;
using HomeAssistant.Modules.Interfaces;

namespace Assistant.Modules.Interfaces {

	public interface IDynamicWatcher {

		ILoggerBase Logger { get; set; }

		string DirectoryToWatch { get; set; }

		FileSystemWatcher FileSystemWatcher { get; set; }

		bool WatcherOnline { get; set; }

		bool IncludeSubdirectories { get; set; }

		(bool, DynamicWatcher) InitWatcherService();

		void StopWatcherServier();

		void OnFileDeleted(object sender, FileSystemEventArgs e);

		void OnFileRenamed(object sender, RenamedEventArgs e);

		void OnFileChanged(object sender, FileSystemEventArgs e);

		void OnFileCreated(object sender, FileSystemEventArgs e);

	}
}
