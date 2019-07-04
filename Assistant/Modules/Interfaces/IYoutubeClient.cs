using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {
	public interface IYoutubeClient : IModuleBase {
		/// <summary>
		/// Downloads the video from the specified url
		/// </summary>
		/// <param name="url">The url of the video to download</param>
		/// <param name="saveLocation">The location to save the downloaded file</param>
		/// <returns></returns>
		Task DownloadVideo (string url, string saveLocation);
		/// <summary>
		/// Fetchs the mp3 sound from the video
		/// </summary>
		/// <param name="videoUrl">The url of the video</param>
		/// <param name="saveLocation">The location to save the mp3 file</param>
		/// <returns></returns>
		Task FetchMp3FromVideo (string videoUrl, string saveLocation);

	}
}
