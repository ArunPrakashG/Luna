using Assistant.Extensions;
using Assistant.Extensions.Attributes;
using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Pushbullet.Exceptions;
using Assistant.Pushbullet.Models;
using Assistant.Pushbullet.Parameters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;
using static Assistant.Pushbullet.PushEnums;

namespace Assistant.Pushbullet {
	public class PushbulletClient : IExternal, IDisposable {
		private static int RequestFailedCount = 0;

		private const int MAX_TRIES = 2;
		private const int DELAY_BETWEEN_FAILED_REQUEST = 30; // secs
		private const int DELAY_BETWEEN_REQUEST = 10; // secs
		private const int RATE_LIMITED_DELAY = 10; // In minutes
		private const string API_BASE_URL_NO_VERSION = "https://api.pushbullet.com/";
		private const string API_BASE_VERSION = "v2/";
		private const string API_BASE_URL = API_BASE_URL_NO_VERSION + API_BASE_VERSION;
		private const int MAX_REQUEST_FAILED_COUNT = 3;
		private readonly ILogger Logger = new Logger(typeof(PushbulletClient).Name);
		private static readonly SemaphoreSlim RequestSync = new SemaphoreSlim(1, 1);

		private readonly ClientConfig Config;
		private readonly HttpClientHandler HttpClientHandler;
		private readonly HttpClient HttpClient;

		public bool IsServiceLoaded { get; private set; }

		public PushbulletClient(ClientConfig _config, HttpClientHandler _clientHandler = null) {
			Config = _config;

			HttpClientHandler = _clientHandler != null
				? _clientHandler
				: new HttpClientHandler() {
					Proxy = Config.ShouldUseProxy ? Config.Proxy : null,
					UseProxy = Config.ShouldUseProxy,
					AllowAutoRedirect = false
				};

			HttpClient = new HttpClient(HttpClientHandler);
			HttpClient.DefaultRequestHeaders.Add("Access-Token", Config.AccessToken);
			HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
			IsServiceLoaded = true;
		}

		public async IAsyncEnumerable<Device> GetDevicesAsync() {
			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_DEVICES);
			Response result = await InternalUrlEncodedRequestToObject<Response>(requestUrl, HttpMethod.Get, null).ConfigureAwait(false) ?? throw new RequestFailedException(nameof(GetDevicesAsync) + " request failed.");
			
			for (int i = 0; i < result.Devices.Length; i++) {
				if (result.Devices[i] == null) {
					continue;
				}

				yield return result.Devices[i];
			}
		}

		public async Task<Push> PushAsync<T>(PushRequestParameter<T> parameter) {
			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.PUSH);
			string jsonContent = JsonConvert.SerializeObject(parameter.PushType);

			if (string.IsNullOrEmpty(jsonContent)) {
				throw new ParameterValueIsNullException(nameof(parameter.PushType));
			}

			dynamic jObject = JObject.Parse(jsonContent);
			switch (parameter.PushTarget) {
				case PushTarget.All:
					// ignore
					break;
				case PushTarget.Email:
					jObject.email = parameter.PushTargetValue;
					break;
				case PushTarget.Channel:
					jObject.channel_tag = parameter.PushTargetValue;
					break;
				case PushTarget.Client:
					jObject.client_iden = parameter.PushTargetValue;
					break;
				case PushTarget.Device:
					jObject.device_iden = parameter.PushTargetValue;
					break;
			}

			jsonContent = JsonConvert.SerializeObject(jObject);
			Push result = await InternalStringContentRequestToObject<Push>(requestUrl, HttpMethod.Post, jsonContent).ConfigureAwait(false) ?? throw new RequestFailedException(nameof(PushAsync) + " request failed.");			
			return result;
		}
				
		public async IAsyncEnumerable<Subscription> GetSubscriptionsAsync() {
			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_SUBSCRIPTIONS);
			Response result = await InternalUrlEncodedRequestToObject<Response>(requestUrl, HttpMethod.Get, null).ConfigureAwait(false) ?? throw new RequestFailedException(nameof(GetSubscriptionsAsync) + " request failed.");

			for (int i = 0; i < result.Subscriptions.Length; i++) {
				if (result.Subscriptions[i] == null) {
					continue;
				}

				yield return result.Subscriptions[i];
			}
		}
		
		public async Task<PushDeleteStatusCode> DeletePushAsync(string pushIdentifier) {
			if (string.IsNullOrEmpty(pushIdentifier)) {
				return PushDeleteStatusCode.Unknown;
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.DELETE_PUSH) + pushIdentifier;
			Response response = await InternalUrlEncodedRequestToObject<Response>(requestUrl, HttpMethod.Delete, null).ConfigureAwait(false) ?? throw new RequestFailedException(nameof(DeletePushAsync) + " request failed");
			return response.IsDeleteRequestSuccess ? PushDeleteStatusCode.Success : PushDeleteStatusCode.Unknown;
		}
		
		public async IAsyncEnumerable<Push> GetAllPushesAsync(PushListRequestParameter requestParams) {
			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_ALL_PUSHES);

			Dictionary<string, string> data = new Dictionary<string, string>(4) {
				{"cursor", requestParams.Cursor ?? "" },
				{"modified_after", requestParams.ModifiedAfter.ToString() ?? "1400000000" },
				{"limit", requestParams.MaxResults.ToString() },
				{"active", requestParams.ActiveOnly.ToString() }
			};

			Response result = await InternalUrlEncodedRequestToObject<Response>(requestUrl, HttpMethod.Get, data).ConfigureAwait(false) ?? throw new RequestFailedException(nameof(GetAllPushesAsync) + " request failed.");

			for(int i = 0; i < result.Pushes.Length; i++) {
				if(result.Pushes[i] == null) {
					continue;
				}

				yield return result.Pushes[i];
			}
		}
		
		public async Task<ChannelInfo> GetChannelInfoAsync(string channelTag, bool ignoreRecentPushes = false) {
			if (string.IsNullOrEmpty(channelTag)) {
				throw new ArgumentNullException(nameof(channelTag));
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_CHANNEL_INFO);
			Dictionary<string, string> data = new Dictionary<string, string>(2) {
				{"tag", channelTag },
				{"no_recent_pushes", ignoreRecentPushes.ToString() }
			};

			return await InternalUrlEncodedRequestToObject<ChannelInfo>(requestUrl, HttpMethod.Get, data).ConfigureAwait(false) ?? throw new RequestFailedException(nameof(GetChannelInfoAsync) + " request failed.");
		}

		private string GetRoute(EPUSH_ROUTES route) {
			return route switch
			{
				EPUSH_ROUTES.GET_ALL_PUSHES => "pushes/",
				EPUSH_ROUTES.GET_CHANNEL_INFO => "channel-info/",
				EPUSH_ROUTES.GET_DEVICES => "devices/",
				EPUSH_ROUTES.GET_SUBSCRIPTIONS => "subscriptions/",
				EPUSH_ROUTES.PUSH => "pushes/",
				EPUSH_ROUTES.DELETE_PUSH => "pushes/",
				_ => "pushes/",
			};
		}

		private async Task<T> InternalStringContentRequestToObject<T>(string requestUrl, HttpMethod method, string data, int maxTries = MAX_TRIES) {
			if (string.IsNullOrEmpty(requestUrl)) {
				return default;
			}

			bool success = false;
			for (int i = 0; i < maxTries; i++) {
				try {
					using (HttpRequestMessage request = new HttpRequestMessage(method, requestUrl)) {
						if (!string.IsNullOrEmpty(data)) {
							request.Content = new StringContent(data);
						}

						using (HttpResponseMessage response = await ExecuteRequest(async () => await HttpClient.SendAsync(request).ConfigureAwait(false)).ConfigureAwait(false)) {
							if (!response.IsSuccessStatusCode) {
								continue;
							}

							using (HttpContent responseContent = response.Content) {
								string jsonContent = await responseContent.ReadAsStringAsync().ConfigureAwait(false);

								if (string.IsNullOrEmpty(jsonContent)) {
									continue;
								}

								success = true;
								return JsonConvert.DeserializeObject<T>(jsonContent);
							}
						}
					}
				}
				catch (Exception e) {
					Logger.Exception(e);
					success = false;
					continue;
				}
				finally {
					if (!success) {
						await Task.Delay(TimeSpan.FromSeconds(DELAY_BETWEEN_FAILED_REQUEST)).ConfigureAwait(false);
					}
				}
			}

			if (!success) {
				Logger.Error("Internal request failed.");
			}

			return default;
		}

		private async Task<T> InternalUrlEncodedRequestToObject<T>(string requestUrl, HttpMethod method, Dictionary<string, string> data, int maxTries = MAX_TRIES) {
			if (string.IsNullOrEmpty(requestUrl)) {
				return default;
			}

			bool success = false;
			for (int i = 0; i < maxTries; i++) {
				try {
					using (HttpRequestMessage request = new HttpRequestMessage(method, requestUrl)) {
						if (data != null && data.Count > 0) {
							request.Content = new FormUrlEncodedContent(data);
						}

						using (HttpResponseMessage response = await ExecuteRequest(async () => await HttpClient.SendAsync(request).ConfigureAwait(false)).ConfigureAwait(false)) {
							if (!response.IsSuccessStatusCode) {
								continue;
							}

							using (HttpContent responseContent = response.Content) {
								string jsonContent = await responseContent.ReadAsStringAsync().ConfigureAwait(false);

								if (string.IsNullOrEmpty(jsonContent)) {
									continue;
								}

								success = true;
								return JsonConvert.DeserializeObject<T>(jsonContent);
							}
						}
					}
				}
				catch (Exception e) {
					Logger.Exception(e);
					success = false;
					continue;
				}
				finally {
					if (!success) {
						await Task.Delay(TimeSpan.FromSeconds(DELAY_BETWEEN_FAILED_REQUEST)).ConfigureAwait(false);
					}
				}
			}

			if (!success) {
				Logger.Error("Internal request failed.");
			}

			return default;
		}

		private async Task<T> ExecuteRequest<T>(Func<Task<T>> function) {
			if (function == null) {
				return default;
			}

			await RequestSync.WaitAsync().ConfigureAwait(false);

			try {
				return await function().ConfigureAwait(false);
			}
			finally {
				await Task.Delay(TimeSpan.FromSeconds(DELAY_BETWEEN_REQUEST));
				RequestSync.Release();
			}
		}

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);

		public void Dispose() {
			RequestSync?.Dispose();
			HttpClientHandler?.Dispose();
			HttpClient?.Dispose();
		}
	}
}
