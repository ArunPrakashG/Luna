using System.IO;
using System.Threading.Tasks;

namespace Assistant.Modules.Interfaces.EventInterfaces {

	/// <summary>
	/// Provides various AsyncTask methods which is fired during certain events occurring in assistant.
	/// </summary>
	public interface IAsyncEventBase : IModuleBase {

		/// <summary>
		/// Fired when assistant core initiation is completed.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnAssistantStartedAsync();

		/// <summary>
		/// Fired when a shutdown method is called during assistant runtime.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnAssistantShutdownRequestedAsync();

		/// <summary>
		/// Fired when an update is available for assistant.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnUpdateAvailableAsync();

		/// <summary>
		/// Fired when the update process is started for assistant.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnUpdateStartedAsync();

		/// <summary>
		/// Fired when Internet connection is lost.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnNetworkDisconnectedAsync();

		/// <summary>
		/// Fired when assistant is back online.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnNetworkReconnectedAsync();

		/// <summary>
		/// Fired when SystemShutdown() method is invoked.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnSystemShutdownRequestedAsync();

		/// <summary>
		/// Fired when SystemRestart() method is invoked.
		/// </summary>
		/// <returns></returns>
		Task<bool> OnSystemRestartRequestedAsync();

		/// <summary>
		/// Fired when watcher event raised for specified directory (Deleted/Modified/Renamed)
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event data</param>
		/// <returns></returns>
		Task<bool> OnWatcherEventRasiedAsync(object sender, FileSystemEventArgs e);

	}
}
