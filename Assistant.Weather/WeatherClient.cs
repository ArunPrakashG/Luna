using Assistant.Extensions;
using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Weather {
	public class WeatherClient : IExternal {
		private const int MAX_RETRY_COUNT = 3;
		public WeatherResponse Response { get; private set; } = new WeatherResponse();
		private readonly ILogger Logger = new Logger(typeof(WeatherClient).Name);
		private static readonly HttpClient Client = new HttpClient();
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		private string? GenerateRequestUrl(string apiKey, int pinCode, string countryCode = "in") {
			if (string.IsNullOrEmpty(apiKey) || pinCode <= 0 || string.IsNullOrEmpty(countryCode)) {
				return null;
			}

			return $"https://api.openweathermap.org/data/2.5/weather?zip={pinCode},{countryCode}&appid={apiKey}";
		}

		public async Task<WeatherResponse?> GetWeather(string? apiKey, int pinCode, string? countryCode = "in") {
			if (!Helpers.IsNetworkAvailable()) {
				Logger.Warning("Networking isn't available.");
				return null;
			}

			if (string.IsNullOrEmpty(apiKey) || pinCode <= 0 || string.IsNullOrEmpty(countryCode)) {
				return null;
			}

			return await Request(apiKey, pinCode, countryCode).ConfigureAwait(false);
		}

		private async Task<WeatherResponse?> Request(string apiKey, int pinCode = 689653, string countryCode = "in") {
			if (!Helpers.IsNetworkAvailable()) {
				Logger.Warning("Networking isn't available.");
				return null;
			}

			if (string.IsNullOrEmpty(apiKey) || pinCode <= 0 || string.IsNullOrEmpty(countryCode)) {
				return null;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			WeatherResponse? response = default;
			try {
				int requestCount = 0;
				while (requestCount < MAX_RETRY_COUNT) {
					HttpResponseMessage httpResponse = await Client.GetAsync(GenerateRequestUrl(apiKey, pinCode, countryCode)).ConfigureAwait(false);

					if (httpResponse == null || httpResponse.StatusCode != HttpStatusCode.OK) {
						requestCount++;
						continue;
					}

					string? responseString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

					if (string.IsNullOrEmpty(responseString)) {
						requestCount++;
						continue;
					}

					response = JsonConvert.DeserializeObject<WeatherResponse>(responseString);
					break;
				}

				if (response != null) {
					Logger.Trace("Weather data request success!");
					return response;
				}

				Logger.Warning($"Weather request failed after {requestCount} tries.");
				return null;
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
	}
}
