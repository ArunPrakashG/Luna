using Luna.Extensions;
using Luna.Extensions.Interfaces;
using Luna.Logging;
using Luna.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Weather {
	public class WeatherClient : IExternal, IDisposable {
		private const int MAX_RETRY_COUNT = 3;
		private readonly ILogger Logger = new Logger(typeof(WeatherClient).Name);
		private readonly HttpClient Client = new HttpClient();
		private readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		private readonly string AccessToken;
		private readonly int LocationCode;
		private readonly string CountryCode;

		public WeatherClient(string accessToken, int locPinCode, string countryCode = "in") {
			AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken) + " cannot be null!");
			LocationCode = locPinCode > 0 ? locPinCode : throw new ArgumentOutOfRangeException(nameof(locPinCode) + " should be greater than 0, and must be a valid pin code!");
			CountryCode = countryCode ?? throw new ArgumentNullException(nameof(countryCode) + " country code cannot be null!");
		}

		private string GenerateRequestUrl() => $"https://api.openweathermap.org/data/2.5/weather?zip={LocationCode},{CountryCode}&appid={AccessToken}";

		public async Task<WeatherResponse?> GetAsync() {
			if (!Helpers.IsNetworkAvailable()) {
				Logger.Warning("Networking isn't available.");
				return null;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				for (int i = 0; i < MAX_RETRY_COUNT; i++) {
					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GenerateRequestUrl())) {
						using (HttpResponseMessage response = await Client.SendAsync(request).ConfigureAwait(false)) {
							if (response.StatusCode == HttpStatusCode.GatewayTimeout || response.StatusCode == HttpStatusCode.RequestTimeout) {
								continue;
							}

							string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

							if (string.IsNullOrEmpty(responseString)) {
								continue;
							}

							return JsonConvert.DeserializeObject<WeatherResponse>(responseString);
						}
					}
				}

				return default;
			}
			catch (Exception e) {
				Logger.Log(e);
				return null;
			}
			finally {
				Sync.Release();
			}
		}

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);

		public void Dispose() {
			Client?.Dispose();
			Sync?.Dispose();
		}
	}
}
