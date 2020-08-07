using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Luna.Modules.Interfaces {
	/// <summary>
	/// Provides various async methods which is fired during certain events occurring in assistant.
	/// </summary>
	public interface IAsyncEvent {

		/// <summary>
		/// Fired when assistant core initiation is completed.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnStarted();

		/// <summary>
		/// Fired when a shutdown method is called during assistant runtime.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnShutdownRequested();

		/// <summary>
		/// Fired when an update is available for assistant.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnUpdateAvailable();

		/// <summary>
		/// Fired when the update process is started for assistant.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnUpdateStarted();

		/// <summary>
		/// Fired when Internet connection is lost.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnNetworkDisconnected();

		/// <summary>
		/// Fired when assistant is back online.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnNetworkReconnected();

		/// <summary>
		/// Fired when SystemShutdown() method is invoked.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnSystemShutdownRequested();

		/// <summary>
		/// Fired when SystemRestart() method is invoked.
		/// </summary>
		/// <returns></returns>
		public abstract Task OnSystemRestartRequested();

		/// <summary>
		/// Fired when watcher event raised for specified directory (Deleted/Modified/Renamed)
		/// </summary>		
		/// <returns></returns>
		public abstract Task OnWatcherEventRasied();
	}
}
