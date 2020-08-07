using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Luna.Modules.Interfaces {

	/// <summary>
	/// Provides various methods which is fired during certain events occurring in assistant.
	/// </summary>
	public interface IEvent {

		/// <summary>
		/// Fired when assistant core initiation is completed.
		/// </summary>
		/// <returns></returns>
		public abstract void OnStarted();

		/// <summary>
		/// Fired when a shutdown method is called during assistant runtime.
		/// </summary>
		/// <returns></returns>
		public abstract void OnShutdownRequested();

		/// <summary>
		/// Fired when an update is available for assistant.
		/// </summary>
		/// <returns></returns>
		public abstract void OnUpdateAvailable();

		/// <summary>
		/// Fired when the update process is started for assistant.
		/// </summary>
		/// <returns></returns>
		public abstract void OnUpdateStarted();

		/// <summary>
		/// Fired when Internet connection is lost.
		/// </summary>
		/// <returns></returns>
		public abstract void OnNetworkDisconnected();

		/// <summary>
		/// Fired when assistant is back online.
		/// </summary>
		/// <returns></returns>
		public abstract void OnNetworkReconnected();

		/// <summary>
		/// Fired when SystemShutdown() method is invoked.
		/// </summary>
		/// <returns></returns>
		public abstract void OnSystemShutdownRequested();

		/// <summary>
		/// Fired when SystemRestart() method is invoked.
		/// </summary>
		/// <returns></returns>
		public abstract void OnSystemRestartRequested();

		/// <summary>
		/// Fired when watcher event raised for specified directory (Deleted/Modified/Renamed)
		/// </summary>		
		/// <returns></returns>
		public abstract void OnWatcherEventRasied();
	}
}
