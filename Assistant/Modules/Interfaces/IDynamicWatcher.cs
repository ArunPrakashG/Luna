using HomeAssistant.AssistantCore;
using HomeAssistant.Modules.Interfaces;
using System.IO;

namespace Assistant.Modules.Interfaces {

	public interface IDynamicWatcher {

		ILoggerBase Logger { get; set; }

		string DirectoryToWatch { get; set; }

		FileSystemWatcher FileSystemWatcher { get; set; }

		bool WatcherOnline { get; set; }

		bool IncludeSubdirectories { get; set; }

		(bool, DynamicWatcher, FileSystemWatcher) InitWatcherService();

		void StopWatcherServier();

		void OnFileDeleted(object sender, FileSystemEventArgs e);

		void OnFileRenamed(object sender, RenamedEventArgs e);

		void OnFileChanged(object sender, FileSystemEventArgs e);

		void OnFileCreated(object sender, FileSystemEventArgs e);
	}
}
