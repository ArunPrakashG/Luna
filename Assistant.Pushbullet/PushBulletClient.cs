using Assistant.Extensions;
using Assistant.Extensions.Attributes;
using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Pushbullet.Exceptions;
using Assistant.Pushbullet.Parameters;
using Assistant.Pushbullet.Responses;
using Assistant.Pushbullet.Responses.Devices;
using Assistant.Pushbullet.Responses.Pushes;
using Assistant.Pushbullet.Responses.Subscriptions;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

		private static DateTime LastRequestTime = DateTime.Now.AddMinutes(-RATE_LIMITED_DELAY);
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

		public async Task<DevicesBase[]?> GetDevicesAsync() {
			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_DEVICES);
			ResponseBase result = await InternalRequestToJsonObject<ResponseBase>(requestUrl, HttpMethod.Get, null).ConfigureAwait(false);

			if (result == null || result.Devices == null) {
				throw new RequestFailedException();
			}

			Logger.Log(nameof(GetDevicesAsync) + " Successful.", LogLevels.Trace);
			return result.Devices;
		}

		[TODO]
		public async Task<PushesBase[]?> Push(PushRequestContent pushRequestContent) {
			if (pushRequestContent == null) {
				throw new ParameterValueIsNullException("pushMessageValues value is null.");
			}

			if (ClientAccessToken == null || string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.PUSH);
			List<(string headerName, string headerValue)> headers = new List<(string headerName, string headerValue)> {
				("Content-Type", "application/json")
			};

			List<(string paramName, object paramValue, ParameterType paramType)>? parameters = new List<(string paramName, object paramValue, ParameterType paramType)>();

			switch (pushRequestContent.PushTarget) {
				case PushTarget.Device:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("device_iden", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushTarget.Client:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("client_iden", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushTarget.Email:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("email", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushTarget.Channel:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("channel_tag", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushTarget.All:
					parameters = null;
					break;
			}

			switch (pushRequestContent.PushType) {
				case PushType.Note:
					parameters?.Add(("type", "note", ParameterType.RequestBody));

					if (!string.IsNullOrEmpty(pushRequestContent.PushTitle)) {
						parameters?.Add(("title", pushRequestContent.PushTitle, ParameterType.RequestBody));
					}

					if (!string.IsNullOrEmpty(pushRequestContent.PushBody)) {
						parameters?.Add(("body", pushRequestContent.PushBody, ParameterType.RequestBody));
					}

					break;
				case PushType.Link:
					parameters?.Add(("type", "link", ParameterType.RequestBody));

					if (!string.IsNullOrEmpty(pushRequestContent.PushTitle)) {
						parameters?.Add(("title", pushRequestContent.PushTitle, ParameterType.RequestBody));
					}

					if (!string.IsNullOrEmpty(pushRequestContent.PushBody)) {
						parameters?.Add(("body", pushRequestContent.PushBody, ParameterType.RequestBody));
					}

					if (!string.IsNullOrEmpty(pushRequestContent.LinkUrl)) {
						parameters?.Add(("url", pushRequestContent.LinkUrl, ParameterType.RequestBody));
					}

					break;

				//TODO: File support
				//case PushEnums.PushType.File:
				//	bodyParams.Add("type", "file");
				//	if (!string.IsNullOrEmpty(pushRequestContent.FileName)) {
				//		bodyParams.Add("file_name", pushRequestContent.FileName);
				//	}

				//	if (!string.IsNullOrEmpty(pushRequestContent.FileType)) {
				//		bodyParams.Add("file_type", pushRequestContent.FileType);
				//	}

				//	if (!string.IsNullOrEmpty(pushRequestContent.FileUrl)) {
				//		bodyParams.Add("file_url", pushRequestContent.FileUrl);
				//	}

				//	if (!string.IsNullOrEmpty(pushRequestContent.PushBody)) {
				//		bodyParams.Add("body", pushRequestContent.PushBody);
				//	}

				//	break;
				default:
					throw new InvalidRequestException();
			}

			ResponseBase? response = await GetResponseAsync(requestUrl, Method.POST, headers, parameters);

			if (response == null) {
				throw new RequestFailedException();
			}

			Logger.Log(nameof(Push) + " successful.", LogLevels.Trace);
			return response.Pushes;
		}

		[TODO]
		public async Task<SubscriptionsBase[]?> GetSubscriptions() {
			if (string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_SUBSCRIPTIONS);
			var response = await GetResponseAsync(requestUrl, Method.GET, null, null);

			if (response == null) {
				throw new RequestFailedException();
			}

			Logger.Log(nameof(GetSubscriptions) + " successful.", LogLevels.Trace);
			return response.Subscriptions;
		}

		[TODO]
		public async Task<PushDeleteStatusCode> DeletePush(string pushIdentifier) {
			if (string.IsNullOrEmpty(pushIdentifier)) {
				throw new ParameterValueIsNullException("pushIdentifier");
			}

			if (string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.DELETE_PUSH) + pushIdentifier;
			var response = await GetResponseAsync(requestUrl, Method.DELETE, null, null);

			if (response == null) {
				throw new RequestFailedException();
			}

			return response.IsDeleteRequestSuccess ? PushDeleteStatusCode.Success : PushDeleteStatusCode.Unknown;
		}

		[TODO]
		public async Task<PushesBase[]?> GetAllPushes(PushListRequestContent requestParams) {
			if (requestParams == null) {
				throw new ParameterValueIsNullException("listPushParams is null.");
			}

			if (string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.PUSH);
			List<(string paramName, object paramValue, ParameterType paramType)> parameters = new List<(string paramName, object paramValue, ParameterType paramType)>();

			if (!string.IsNullOrEmpty(requestParams.Cursor)) {
				parameters.Add(("cursor", requestParams.Cursor, ParameterType.QueryString));
			}

			if (!string.IsNullOrEmpty(requestParams.ModifiedAfter)) {
				parameters.Add(("modified_after", requestParams.ModifiedAfter, ParameterType.QueryString));
			}

			if (requestParams.MaxResults > 0) {
				parameters.Add(("limit", requestParams.MaxResults.ToString(), ParameterType.QueryString));
			}

			if (requestParams.ActiveOnly) {
				parameters.Add(("active", requestParams.ActiveOnly.ToString(), ParameterType.QueryString));
			}

			var response = await GetResponseAsync(requestUrl, Method.GET, null, parameters);

			if (response == null || response.Pushes == null) {
				throw new RequestFailedException();
			}

			Logger.Log(nameof(GetAllPushes) + " successful.", LogLevels.Trace);
			return response.Pushes;
		}

		[TODO]
		public async Task<ChannelInfoBase?> GetChannelInfo(string channelTag, bool dontRecentPushes = false) {
			if (ClientAccessToken == null || string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			if (string.IsNullOrEmpty(channelTag)) {
				throw new ParameterValueIsNullException("channelTag is null");
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_CHANNEL_INFO);
			List<(string paramName, object paramValue, ParameterType paramType)> parameters = new List<(string paramName, object paramValue, ParameterType paramType)> {
				("tag", channelTag, ParameterType.QueryString),
				("no_recent_pushes", dontRecentPushes, ParameterType.QueryString)
			};

			var response = await GetResponseAsync<ChannelInfoBase>(requestUrl, Method.GET, null, parameters);

			if (response == null) {
				throw new RequestFailedException();
			}

			Logger.Log(nameof(GetChannelInfo) + " successful.", LogLevels.Trace);
			return response;
		}		

		private async Task<TType> GetResponseAsync<TType>(
			string requestUrl,
			Method reqMethod,
			List<(string headerName, string headerValue)>? headers = null,
			List<(string paramName, object paramValue, ParameterType paramType)>? parameters = null
			) {
			if (string.IsNullOrEmpty(requestUrl)) {
				Logger.Log("Request URL cannot be empty.", LogLevels.Warn);
				return default;
			}

			if (!Helpers.IsNetworkAvailable()) {
				throw new RequestFailedException("No Internet connectivity");
			}

			if (RestClient == null) {
				SetClient();
			}

			await RequestSync.WaitAsync().ConfigureAwait(false);

			try {
				if (RestClient == null) {
					throw new RequestFailedException("RestClient is null.");
				}

				while (RequestInSleepMode) {
					Logger.Log("Request is in sleep mode. waiting...", LogLevels.Trace);
					await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
				}

				if (RequestFailedCount > MAX_REQUEST_FAILED_COUNT) {
					RequestInSleepMode = true;
					Logger.Log($"Requests have failed multiple times... Sleeping for {RATE_LIMITED_DELAY} minute(s).", LogLevels.Warn);
					Helpers.ScheduleTask(() => {
						RequestFailedCount = 0;
						RequestInSleepMode = false;
					}, TimeSpan.FromMinutes(RATE_LIMITED_DELAY));
					return default;
				}

				int currentCount = 0;
				IRestResponse? response = default;

				while (currentCount < MAX_REQUEST_FAILED_COUNT) {
					RestRequest request = GenerateRequest(reqMethod, headers, parameters);
					response = await Request<IRestResponse>(async () => await RestClient.ExecuteAsync(request).ConfigureAwait(false));

					if (response == null || string.IsNullOrEmpty(response.Content)) {
						Logger.Log($"Unknown error has occurred during request. Request Count -> {currentCount}", LogLevels.Error);
						currentCount++;
						continue;
					}

					if (response.StatusCode != HttpStatusCode.OK) {
						Logger.Log($"Request Failed. Status Code: " + response.StatusCode + "/" + response.ResponseStatus, LogLevels.Error);
						break;
					}

					if (response.IsSuccessful) {
						Logger.Log("Request success.");
						break;
					}
				}

				if (response == null || string.IsNullOrEmpty(response.Content)) {
					return default;
				}

				TType objectType = default;

				try {
					objectType = JsonConvert.DeserializeObject<TType>(response.Content);
				}
				catch (Exception) {
					Logger.Log("Could not parse response as json of the requested type. Parsing as InvalidResponse...", LogLevels.Error);
					InvalidResponse errorResponse = JsonConvert.DeserializeObject<InvalidResponse>(response.Content);

					if (errorResponse != null && errorResponse.ErrorObject != null) {
						Logger.Log($"--------------------> API ERROR INFO <--------------------", LogLevels.Error);
						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Message)) {
							Logger.Log($"Message -> {errorResponse.ErrorObject.Message}", LogLevels.Error);
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Type)) {
							Logger.Log($"Type -> {errorResponse.ErrorObject.Type}", LogLevels.Error);
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Cat)) {
							Logger.Log($"Type -> {errorResponse.ErrorObject.Cat}", LogLevels.Error);
						}

						Logger.Log($"-------------------- <-> --------------------", LogLevels.Error);
					}

					return default;
				}

				return objectType;
			}
			catch (Exception e) {
				Logger.Log(e);
				return default;
			}
			finally {
				RequestSync.Release();
			}
		}

		private async Task<ResponseBase?> GetResponseAsync(string requestUrl, Method reqMethod, List<(string headerName, string headerValue)>? headers = null, List<(string paramName, object paramValue, ParameterType paramType)>? parameters = null) {
			if (string.IsNullOrEmpty(requestUrl)) {
				Logger.Log("Request url cannot be empty.", LogLevels.Warn);
				return default;
			}

			if (!Helpers.IsNetworkAvailable()) {
				throw new RequestFailedException("No Internet connectivity");
			}

			if (RestClient == null) {
				SetClient();
			}

			await RequestSync.WaitAsync().ConfigureAwait(false);

			try {
				if (RestClient == null) {
					throw new RequestFailedException("RestClient is null.");
				}

				while (RequestInSleepMode) {
					Logger.Log("Request is in sleep mode. Waiting...", LogLevels.Trace);
					await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
				}

				if (RequestFailedCount > MAX_REQUEST_FAILED_COUNT) {
					RequestInSleepMode = true;
					Logger.Log($"Requests have failed multiple times... Sleeping for {RATE_LIMITED_DELAY} minute(s).", LogLevels.Warn);
					Helpers.ScheduleTask(() => {
						RequestFailedCount = 0;
						RequestInSleepMode = false;
					}, TimeSpan.FromMinutes(RATE_LIMITED_DELAY));
					return default;
				}

				int currentCount = 0;
				IRestResponse? response = default;

				while (currentCount < MAX_REQUEST_FAILED_COUNT) {
					RestRequest request = GenerateRequest(reqMethod, headers, parameters);
					response = await Request<IRestResponse>(async () => await RestClient.ExecuteAsync(request).ConfigureAwait(false));

					if (response == null || string.IsNullOrEmpty(response.Content)) {
						Logger.Log($"Unknown error has occurred during request. Request Count -> {currentCount}", LogLevels.Error);
						currentCount++;
						continue;
					}

					if (response.StatusCode != HttpStatusCode.OK) {
						Logger.Log($"Request Failed. Status Code: " + response.StatusCode + "/" + response.ResponseStatus, LogLevels.Warn);
						break;
					}

					if (response.IsSuccessful) {
						Logger.Log("Request success.", LogLevels.Info);
						break;
					}
				}

				if (response != null && string.IsNullOrEmpty(response.Content) && response.StatusCode == HttpStatusCode.OK) {
					ResponseBase deleteRequestResponse = new ResponseBase {
						IsDeleteRequestSuccess = true
					};
					return deleteRequestResponse;
				}

				if (response == null || string.IsNullOrEmpty(response.Content)) {
					return default;
				}

				ResponseBase? objectType = default;

				try {
					objectType = JsonConvert.DeserializeObject<ResponseBase>(response.Content);
				}
				catch (Exception) {
					Logger.Log("Could not parse response as json of the requested type. Parsing as InvalidResponse...", LogLevels.Error);
					InvalidResponse errorResponse = JsonConvert.DeserializeObject<InvalidResponse>(response.Content);

					if (errorResponse != null && errorResponse.ErrorObject != null) {
						Logger.Log($"--------------------> API ERROR INFO <--------------------", LogLevels.Error);
						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Message)) {
							Logger.Log($"Message -> {errorResponse.ErrorObject.Message}", LogLevels.Error);
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Type)) {
							Logger.Log($"Type -> {errorResponse.ErrorObject.Type}", LogLevels.Error);
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Cat)) {
							Logger.Log($"Type -> {errorResponse.ErrorObject.Cat}", LogLevels.Error);
						}

						Logger.Log($"-------------------- <-> --------------------", LogLevels.Error);
					}

					return default;
				}

				return objectType;
			}
			catch (Exception e) {
				Logger.Log(e);
				return default;
			}
			finally {
				RequestSync.Release();
			}
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

		private RestRequest GenerateRequest(Method reqMethod, List<(string headerName, string headerValue)>? headers, List<(string paramName, object paramValue, ParameterType paramType)>? parameter) {
			RestRequest request = new RestRequest(reqMethod) {
				RequestFormat = DataFormat.Json
			};

			if (headers != null && headers.Count > 0) {
				foreach ((string headerName, string headerValue) in headers) {
					request.AddHeader(headerName, headerValue);
					Logger.Log($"Added header -> {headerName}", LogLevels.Trace);
				}
			}

			if (parameter != null && parameter.Count > 0) {
				foreach ((string paramName, object paramValue, ParameterType paramType) in parameter) {
					if (!string.IsNullOrEmpty(paramName) && paramValue != null) {
						request.AddParameter(paramName, paramValue, paramType);
						Logger.Log($"Added parameter -> {paramName}", LogLevels.Trace);
					}
				}
			}

			request.AddHeader("Access-Token", ClientAccessToken);
			request.AddHeader("Content-Type", "application/json");
			return request;
		}

		private async Task<T> InternalRequestToJsonObject<T>(string requestUrl, HttpMethod method, Dictionary<string, string> data, int maxTries = MAX_TRIES) {
			if (string.IsNullOrEmpty(requestUrl)) {
				return default;
			}

			bool success = false;
			for (int i = 0; i < maxTries; i++) {
				try {
					using (HttpRequestMessage request = new HttpRequestMessage(method, requestUrl)) {

						if(data != null && data.Count > 0) {
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
	}
}
