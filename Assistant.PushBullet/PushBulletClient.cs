using Assistant.PushBullet.Exceptions;
using Assistant.PushBullet.Logging;
using Assistant.PushBullet.Parameters;
using Assistant.PushBullet.Responses;
using Assistant.PushBullet.Responses.Devices;
using Assistant.PushBullet.Responses.Pushes;
using Assistant.PushBullet.Responses.Subscriptions;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.PushBullet.PushEnums;

namespace Assistant.PushBullet {
	public class PushBulletClient {
		private const int RATE_LIMITED_DELAY = 10; // In minutes
		private const string API_BASE_URL_NO_VERSION = "https://api.pushbullet.com/";
		private const string API_BASE_VERSION = "v2/";
		private const string API_BASE_URL = API_BASE_URL_NO_VERSION + API_BASE_VERSION;
		private const int MAX_REQUEST_FAILED_COUNT = 3;

		private static RestClient? RestClient = new RestClient();
		public string? ClientAccessToken { get; set; }
		public bool IsServiceLoaded { get; private set; }
		private static int RequestFailedCount = 0;
		public bool RequestInSleepMode { get; private set; }
		public static DateTime LastRequestTime { get; private set; }

		private static readonly SemaphoreSlim RequestSemaphore = new SemaphoreSlim(1, 1);

		public PushBulletClient InitPushBulletClient(string? apiKey) {
			if (string.IsNullOrEmpty(apiKey)) {
				EventLogger.LogWarning("No api key specified or the specified api key is invalid.");
				IsServiceLoaded = false;
				throw new IncorrectAccessTokenException();
			}

			ClientAccessToken = apiKey;
			IsServiceLoaded = true;
			SetClient();
			return this;
		}

		public async Task<DevicesBase[]?> GetDevicesAsync() {
			if (ClientAccessToken == null || string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_DEVICES);
			ResponseBase? response = await GetResponseAsync(requestUrl, Method.GET, null, null).ConfigureAwait(false);

			if (response == null || response.Devices == null) {
				throw new RequestFailedException();
			}

			EventLogger.LogInfo(nameof(GetDevicesAsync) + " Successful.");
			return response.Devices;
		}

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

			List<(string paramName, object paramValue, ParameterType paramType)> parameters = new List<(string paramName, object paramValue, ParameterType paramType)>();

			switch (pushRequestContent.PushTarget) {
				case PushEnums.PushTarget.Device:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("device_iden", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushEnums.PushTarget.Client:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("client_iden", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushEnums.PushTarget.Email:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("email", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushEnums.PushTarget.Channel:
					if (!string.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						parameters.Add(("channel_tag", pushRequestContent.PushTargetValue, ParameterType.QueryString));
					}

					break;
				case PushEnums.PushTarget.All:
					parameters = null;
					break;
			}

			switch (pushRequestContent.PushType) {
				case PushEnums.PushType.Note:
					parameters?.Add(("type", "note", ParameterType.RequestBody));

					if (!string.IsNullOrEmpty(pushRequestContent.PushTitle)) {
						parameters?.Add(("title", pushRequestContent.PushTitle, ParameterType.RequestBody));
					}

					if (!string.IsNullOrEmpty(pushRequestContent.PushBody)) {
						parameters?.Add(("body", pushRequestContent.PushBody, ParameterType.RequestBody));
					}

					break;
				case PushEnums.PushType.Link:
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

			EventLogger.LogTrace(nameof(Push) + " successful.");
			return response.Pushes;
		}

		public async Task<SubscriptionsBase[]?> GetSubscriptions() {
			if (string.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException();
			}

			string requestUrl = API_BASE_URL + GetRoute(EPUSH_ROUTES.GET_SUBSCRIPTIONS);
			var response = await GetResponseAsync(requestUrl, Method.GET, null, null);

			if (response == null) {
				throw new RequestFailedException();
			}

			EventLogger.LogTrace(nameof(GetSubscriptions) + " successful.");
			return response.Subscriptions;
		}

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

			EventLogger.LogTrace(nameof(GetAllPushes) + " successful.");
			return response.Pushes;
		}
	
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

			EventLogger.LogTrace(nameof(GetChannelInfo) + " successful.");
			return response;
		}

		private async Task<TType> GetResponseAsync<TType>(string requestUrl, Method reqMethod, List<(string headerName, string headerValue)>? headers = null, List<(string paramName, object paramValue, ParameterType paramType)>? parameters = null) {
			if (string.IsNullOrEmpty(requestUrl)) {
				EventLogger.LogWarning("Request url cannot be empty.");
				return default;
			}

			if (!Helpers.CheckForInternetConnection()) {
				throw new RequestFailedException("No Internet connectivity");
			}

			if (RestClient == null) {
				SetClient();
			}

			await RequestSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				if (RestClient == null) {
					throw new RequestFailedException("RestClient is null.");
				}

				while (RequestInSleepMode) {
					EventLogger.LogTrace("Request is in sleep mode. waiting...");
					await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
				}

				if (RequestFailedCount > MAX_REQUEST_FAILED_COUNT) {
					RequestInSleepMode = true;
					EventLogger.LogWarning($"Requests have failed multiple times... Sleeping for {RATE_LIMITED_DELAY} minute(s).");
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
						EventLogger.LogError($"Unknown error has occurred during request. Request Count -> {currentCount}");
						currentCount++;
						continue;
					}

					if (response.StatusCode != HttpStatusCode.OK) {
						EventLogger.LogWarning($"Request Failed. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
						break;
					}

					if (response.IsSuccessful) {
						EventLogger.LogInfo("Request success.");
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
					EventLogger.LogError("Could not parse response as json of the requested type. Parsing as InvalidResponse...");
					InvalidResponse errorResponse = JsonConvert.DeserializeObject<InvalidResponse>(response.Content);

					if (errorResponse != null && errorResponse.ErrorObject != null) {
						EventLogger.LogError($"--------------------> API ERROR INFO <--------------------");
						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Message)) {
							EventLogger.LogError($"Message -> {errorResponse.ErrorObject.Message}");
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Type)) {
							EventLogger.LogError($"Type -> {errorResponse.ErrorObject.Type}");
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Cat)) {
							EventLogger.LogError($"Type -> {errorResponse.ErrorObject.Cat}");
						}

						EventLogger.LogError($"-------------------- <-> --------------------");
					}

					return default;
				}

				return objectType;
			}
			catch (Exception e) {
				EventLogger.LogException(e);
				return default;
			}
			finally {
				RequestSemaphore.Release();
			}
		}

		private async Task<ResponseBase?> GetResponseAsync(string requestUrl, Method reqMethod, List<(string headerName, string headerValue)>? headers = null, List<(string paramName, object paramValue, ParameterType paramType)>? parameters = null) {
			if (string.IsNullOrEmpty(requestUrl)) {
				EventLogger.LogWarning("Request url cannot be empty.");
				return default;
			}

			if (!Helpers.CheckForInternetConnection()) {
				throw new RequestFailedException("No Internet connectivity");
			}

			if (RestClient == null) {
				SetClient();
			}

			await RequestSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				if (RestClient == null) {
					throw new RequestFailedException("RestClient is null.");
				}

				while (RequestInSleepMode) {
					EventLogger.LogTrace("Request is in sleep mode. waiting...");
					await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
				}

				if (RequestFailedCount > MAX_REQUEST_FAILED_COUNT) {
					RequestInSleepMode = true;
					EventLogger.LogWarning($"Requests have failed multiple times... Sleeping for {RATE_LIMITED_DELAY} minute(s).");
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
						EventLogger.LogError($"Unknown error has occurred during request. Request Count -> {currentCount}");
						currentCount++;
						continue;
					}

					if (response.StatusCode != HttpStatusCode.OK) {
						EventLogger.LogWarning($"Request Failed. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
						break;
					}

					if (response.IsSuccessful) {
						EventLogger.LogInfo("Request success.");
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
					EventLogger.LogError("Could not parse response as json of the requested type. Parsing as InvalidResponse...");
					InvalidResponse errorResponse = JsonConvert.DeserializeObject<InvalidResponse>(response.Content);

					if (errorResponse != null && errorResponse.ErrorObject != null) {
						EventLogger.LogError($"--------------------> API ERROR INFO <--------------------");
						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Message)) {
							EventLogger.LogError($"Message -> {errorResponse.ErrorObject.Message}");
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Type)) {
							EventLogger.LogError($"Type -> {errorResponse.ErrorObject.Type}");
						}

						if (!string.IsNullOrEmpty(errorResponse.ErrorObject.Cat)) {
							EventLogger.LogError($"Type -> {errorResponse.ErrorObject.Cat}");
						}

						EventLogger.LogError($"-------------------- <-> --------------------");
					}

					return default;
				}

				return objectType;
			}
			catch (Exception e) {
				EventLogger.LogException(e);
				return default;
			}
			finally {
				RequestSemaphore.Release();
			}
		}

		private static void SetClient() {
			if (RestClient != null) {
				RestClient = null;
			}

			RestClient = new RestClient(API_BASE_URL);
		}

		private string GetRoute(EPUSH_ROUTES route) {
			switch (route) {
				case EPUSH_ROUTES.GET_ALL_PUSHES:
					return "pushes/";
				case EPUSH_ROUTES.GET_CHANNEL_INFO:
					return "channel-info/";
				case EPUSH_ROUTES.GET_DEVICES:
					return "devices/";
				case EPUSH_ROUTES.GET_SUBSCRIPTIONS:
					return "subscriptions/";
				case EPUSH_ROUTES.PUSH:
					return "pushes/";
				case EPUSH_ROUTES.DELETE_PUSH:
					return "pushes/";
				default:
					return "pushes/";
			}
		}

		private RestRequest GenerateRequest(Method reqMethod, List<(string headerName, string headerValue)>? headers, List<(string paramName, object paramValue, ParameterType paramType)>? parameter) {
			RestRequest request = new RestRequest(reqMethod) {
				RequestFormat = DataFormat.Json
			};

			if (headers != null && headers.Count > 0) {
				foreach ((string headerName, string headerValue) in headers) {
					request.AddHeader(headerName, headerValue);
					EventLogger.LogTrace($"Added header -> {headerName}");
				}
			}

			if (parameter != null && parameter.Count > 0) {
				foreach ((string paramName, object paramValue, ParameterType paramType) in parameter) {
					if (!string.IsNullOrEmpty(paramName) && paramValue != null) {
						request.AddParameter(paramName, paramValue, paramType);
						EventLogger.LogTrace($"Added param -> {paramName}");
					}
				}
			}

			request.AddHeader("Access-Token", ClientAccessToken);
			request.AddHeader("Content-Type", "application/json");
			return request;
		}

		private static async Task<T> Request<T>(Func<Task<T>> function) {
			try {
				await RequestSemaphore.WaitAsync().ConfigureAwait(false);

				if ((DateTime.Now - LastRequestTime).TotalSeconds <= 2) {
					await Task.Delay(TimeSpan.FromMinutes(RATE_LIMITED_DELAY)).ConfigureAwait(false);
				}

				LastRequestTime = DateTime.Now;
				return await function().ConfigureAwait(false);
			}
			catch (Exception e) {
				EventLogger.LogWarning($"Request Exception -> {e.Message}");
				Console.WriteLine(e.Message);
				return default;
			}
			finally {
				RequestSemaphore.Release();
			}
		}

		private static T Request<T>(Func<T> function) {
			try {
				RequestSemaphore.Wait();

				if ((DateTime.Now - LastRequestTime).TotalSeconds <= 3) {
					Task.Delay(TimeSpan.FromMinutes(RATE_LIMITED_DELAY)).Wait();
				}

				LastRequestTime = DateTime.Now;
				return function();
			}
			catch (Exception e) {
				EventLogger.LogWarning($"Request Exception -> {e.Message}");
				return default;
			}
			finally {
				RequestSemaphore.Release();
			}
		}
	}
}
