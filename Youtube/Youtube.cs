
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

using Assistant.Log;
using Assistant.Modules.Interfaces;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using VideoLibrary;

namespace Youtube {

	public class Youtube : IModuleBase, IYoutubeClient {
		private readonly Logger Logger = new Logger("YOUTUBE");

		public Youtube YoutubeInstance { get; set; }
		public string ModulePath { get; set; } = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), nameof(Youtube) + ".dll");

		public bool RequiresInternetConnection { get; set; }

		public string ModuleIdentifier { get; set; }

		public int ModuleType { get; set; } = 3;

		public Version ModuleVersion { get; } = new Version("4.9.0.0");

		public string ModuleAuthor { get; } = "Arun";

		//TODO youtube module
		//fetch youtube video
		//download youtube video
		//search youtube video
		//extract songs

		public Youtube() {
		}

		public bool InitModuleService() {
			RequiresInternetConnection = true;
			YoutubeInstance = this;			
			return true;
		}

		public bool InitModuleShutdown() {
			return true;
		}

		private async Task<YouTubeVideo> FetchYoutubeVideo(string url) {
			YouTubeVideo video = null;

			using (Client<YouTubeVideo> cli = Client.For(new YouTube())) {
				video = await cli.GetVideoAsync(url).ConfigureAwait(false);
				Logger.Log($"Title: {video.Title}");
				Logger.Log($"File extensions: {video.FileExtension}");
				Logger.Log($"Full name: {video.FullName}");
				Logger.Log($"Resolution: {video.Resolution}");
			}
			return video;
		}

		public async Task DownloadVideo(string url, string saveLocation) {
			if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url)) {
				return;
			}

			YouTubeVideo Video = await FetchYoutubeVideo(url).ConfigureAwait(false);
			await File.WriteAllBytesAsync(saveLocation + "/" + Video.FullName, await Video.GetBytesAsync().ConfigureAwait(false)).ConfigureAwait(false);
		}

		public async Task FetchMp3FromVideo(string videoUrl, string saveLocation) {
			YouTubeVideo Video = await FetchYoutubeVideo(videoUrl).ConfigureAwait(false);
			if (Video.AudioFormat == AudioFormat.Aac && Video.AdaptiveKind == AdaptiveKind.Audio && Video.AudioBitrate > 0) {
				byte[] audioBytes = await Video.GetBytesAsync().ConfigureAwait(false);
				await File.WriteAllBytesAsync(saveLocation, audioBytes).ConfigureAwait(false);
			}
		}
	}
}
