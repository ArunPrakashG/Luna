using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Assistant.YoutubeHandler
{
	public class YoutubeHandler : IDisposable
	{
		private readonly ILogger Logger = new Logger(nameof(YoutubeHandler));
		private readonly HttpClientHandler HttpClientHandler;
		private readonly HttpClient HttpClient;
		private readonly YoutubeClient Client;

		public YoutubeHandler() {
			HttpClientHandler = new HttpClientHandler();
			HttpClient = new HttpClient(HttpClientHandler, true);
			Client = new YoutubeClient(HttpClient);
		}

		public YoutubeHandler(IWebProxy _proxy) {
			if(_proxy == null) {
				throw new ArgumentNullException(nameof(_proxy) + " cannot be null!");
			}

			HttpClientHandler = new HttpClientHandler() {
				Proxy = _proxy,
				UseProxy = true
			};

			HttpClient = new HttpClient(HttpClientHandler, true);
			Client = new YoutubeClient(HttpClient);
		}

		public YoutubeHandler(HttpClientHandler _clientHandler) {
			HttpClientHandler = _clientHandler ?? throw new ArgumentNullException(nameof(_clientHandler) + " cannot be null!");
			HttpClient = new HttpClient(HttpClientHandler, true);
			Client = new YoutubeClient(HttpClient);
		}

		public async Task<Video> GetVideoMetadataAsync(string url) {
			if (string.IsNullOrEmpty(url)) {
				return default;
			}

			return await Client.Videos.GetAsync(new VideoId(url)).ConfigureAwait(false);
		}

		public IAsyncEnumerable<Video> SearchVideoAsync(string _query) {
			if (string.IsNullOrEmpty(_query)) {
				return default;
			}

			return await Client.Search.GetVideosAsync(_query);
		}

		public async Task<bool> DownloadVideoToPathAsync(VideoId _videoId, string filePath) {
			if(_videoId == null || string.IsNullOrEmpty(filePath)) {
				return false;				
			}

			StreamManifest streamInfo = await Client.Videos.Streams.GetManifestAsync(_videoId);
			IVideoStreamInfo? videoInfo = streamInfo.GetMuxed().WithHighestVideoQuality();

			if(videoInfo == null) {
				return false;
			}

			await Client.Videos.Streams.DownloadAsync(videoInfo, filePath).ConfigureAwait(false);
			return File.Exists(filePath);
		}

		public void Dispose() => HttpClient?.Dispose();
	}
}
