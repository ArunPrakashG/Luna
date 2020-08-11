using Luna.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.External.Updates {
	internal class GithubResponse : IDisposable {
		[JsonProperty("url")]
		public string? ReleaseUrl { get; set; }

		[JsonProperty("tag_name")]
		public string? ReleaseTagName { get; set; }

		[JsonProperty("name")]
		public string? ReleaseFileName { get; set; }

		[JsonProperty("published_at")]
		public DateTime PublishedAt { get; set; }

		[JsonProperty("assets")]
		public Asset[]? Assets { get; set; }

		public class Asset {
			[JsonProperty("id")]
			public int AssetId { get; set; }

			[JsonProperty("browser_download_url")]
			public string? AssetDownloadUrl { get; set; }
		}

		private const int MAX_TRIES = 3;
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static readonly HttpClient Client = new HttpClient();

		static GithubResponse() {
			Client.DefaultRequestHeaders.Add("User-Agent", Constants.GitProjectName);
		}

		internal async Task<GithubResponse> LoadAsync() {
			if (string.IsNullOrEmpty(Constants.GitReleaseUrl) || Client == null || Sync == null) {
				return this;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				string? json = null;

				for (int i = 0; i < MAX_TRIES; i++) {

					HttpResponseMessage responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Constants.GitReleaseUrl)).ConfigureAwait(false);

					if (responseMessage == null || responseMessage.StatusCode != System.Net.HttpStatusCode.OK || responseMessage.Content == null) {
						Logger.Info($"Request failed ({i})");
						Logger.Info(responseMessage?.ReasonPhrase);
						continue;
					}

					json = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

					if (string.IsNullOrEmpty(json)) {
						continue;
					}

					break;
				}

				if (string.IsNullOrEmpty(json)) {
					return this;
				}

				GithubResponse obj = JsonConvert.DeserializeObject<GithubResponse>(json);
				this.Assets = obj.Assets;
				this.PublishedAt = obj.PublishedAt;
				this.ReleaseFileName = obj.ReleaseFileName;
				this.ReleaseTagName = obj.ReleaseTagName;
				this.ReleaseUrl = obj.ReleaseUrl;
				return this;
			}
			catch (Exception e) {
				Logger.Error(e.ToString());
				return this;
			}
			finally {
				Sync.Release();
			}
		}

		public void Dispose() {
			Client?.Dispose();
			Sync?.Dispose();
		}
	}
}
