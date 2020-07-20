using Luna.Extensions;
using Luna.Logging;
using Luna.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Update {
	internal class GitHub {

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
		private readonly ILogger Logger = new Logger(typeof(GitHub).Name);
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static readonly HttpClient Client = new HttpClient();

		static GitHub() {
			Client.DefaultRequestHeaders.Add("User-Agent", Constants.GitHubProjectName);
		}

		public async Task Request() {
			if (string.IsNullOrEmpty(Constants.GITHUB_RELEASE_URL)) {
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);			

			try {
				string? json = null;

				for (int i = 0; i < MAX_TRIES; i++) {
					
					HttpResponseMessage responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Constants.GITHUB_RELEASE_URL)).ConfigureAwait(false);

					if (responseMessage == null || responseMessage.StatusCode != System.Net.HttpStatusCode.OK || responseMessage.Content == null) {
						Logger.Trace($"Request failed ({i})");
						Logger.Trace(responseMessage?.ReasonPhrase);
						continue;
					}

					json = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

					if (string.IsNullOrEmpty(json)) {
						continue;
					}

					break;
				}

				if (string.IsNullOrEmpty(json)) {
					return;
				}

				GitHub obj = JsonConvert.DeserializeObject<GitHub>(json);
				this.Assets = obj.Assets;
				this.PublishedAt = obj.PublishedAt;
				this.ReleaseFileName = obj.ReleaseFileName;
				this.ReleaseTagName = obj.ReleaseTagName;
				this.ReleaseUrl = obj.ReleaseUrl;

			}
			catch (Exception e) {
				Logger.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}
	}
}
