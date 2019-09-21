
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

using System.IO;
using System.Threading.Tasks;

namespace Assistant.Modules.Interfaces.EventInterfaces {

	/// <summary>
	/// Provides various AsyncTask methods which is fired during certain events occuring in assistant.
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
		/// Fired when internet connection is lost.
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
		/// Fired when any of the config files have been Deleted/Modified/Renamed.
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event data</param>
		/// <returns></returns>
		Task<bool> OnConfigWatcherEventRasiedAsync(object sender, FileSystemEventArgs e);

		/// <summary>
		/// Fired when any of the modules have been Updated/Changed/Deleted/Renamed.
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event data</param>
		/// <returns></returns>
		Task<bool> OnModuleWatcherEventRasiedAsync(object sender, FileSystemEventArgs e);

	}
}
