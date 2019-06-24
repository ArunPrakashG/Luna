using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {
	public interface IYoutubeClient : IModuleBase {

		Task DownloadVideo (string url, string saveLocation);

		Task FetchMp3FromVideo (string videoUrl, string saveLocation);

	}
}
