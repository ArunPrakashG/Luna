using System;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.Modules.Interfaces.EventInterfaces {

	/// <summary>
	/// Provides various AsyncTask methods which is fired during certain events occurring in assistant.
	/// </summary>
	public interface IEvent : IModuleBase {

		/// <summary>
		/// Fired when assistant core initiation is completed.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnAssistantStartedAsync();

		/// <summary>
		/// Fired when a shutdown method is called during assistant runtime.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnAssistantShutdownRequestedAsync();

		/// <summary>
		/// Fired when an update is available for assistant.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnUpdateAvailableAsync();

		/// <summary>
		/// Fired when the update process is started for assistant.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnUpdateStartedAsync();

		/// <summary>
		/// Fired when Internet connection is lost.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnNetworkDisconnectedAsync();

		/// <summary>
		/// Fired when assistant is back online.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnNetworkReconnectedAsync();

		/// <summary>
		/// Fired when SystemShutdown() method is invoked.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnSystemShutdownRequestedAsync();

		/// <summary>
		/// Fired when SystemRestart() method is invoked.
		/// </summary>
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnSystemRestartRequestedAsync();

		/// <summary>
		/// Fired when watcher event raised for specified directory (Deleted/Modified/Renamed)
		/// </summary>		
		/// <returns></returns>
		Func<EventParameter, EventResponse> OnWatcherEventRasiedAsync();

	}
}
