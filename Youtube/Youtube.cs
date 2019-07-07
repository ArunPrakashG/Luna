using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using VideoLibrary;

namespace Youtube {

	public class Youtube : IModuleBase, IYoutubeClient {
		private readonly Logger Logger = new Logger("YOUTUBE");

		public Youtube YoutubeInstance { get; set; }

		public bool RequiresInternetConnection { get; set; }

		public long ModuleIdentifier { get; set; }

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
