using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.PushBullet.ApiResponse;
using Assistant.PushBullet.Exceptions;
using Assistant.PushBullet.Interfaces;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using Assistant.PushBullet.Parameters;
using static Assistant.AssistantCore.Enums;

namespace Assistant.PushBullet {
	public class PushBulletClient {
		private readonly Logger Logger = new Logger("PUSH-BULLET-CLIENT");
		public string ClientAccessToken { get; set; }
		public bool IsServiceLoaded { get; private set; }
		public int ApiFailedCount { get; private set; }
		public bool RequestInSleepMode { get; private set; }

		public PushBulletClient(string apiKey) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				throw new IncorrectAccessTokenException(apiKey);
			}

			ClientAccessToken = apiKey;
			IsServiceLoaded = true;
		}

		public PushBulletClient() {
			if (Helpers.IsNullOrEmpty(Core.Config.PushBulletApiKey)) {
				throw new IncorrectAccessTokenException(Core.Config.PushBulletApiKey);
			}

			Logger.Log("Using default selection of api key as it is not specified.", LogLevels.Warn);
			ClientAccessToken = Core.Config.PushBulletApiKey;
			IsServiceLoaded = true;
		}

		public UserDeviceListResponse GetCurrentDevices() {
			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/devices";
			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.GET);

			if (!requestStatus && Helpers.IsNullOrEmpty(response)) {
				return null;
			}

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched all devices!", LogLevels.Trace);
			return DeserializeJsonObject<UserDeviceListResponse>(response);
		}

		public PushResponse SendPush(PushRequestContent pushRequestContent) {
			if (pushRequestContent == null) {
				throw new ParameterValueIsNullException("pushMessageValues value is null.");
			}

			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/pushes";
			Dictionary<string, string> queryString = new Dictionary<string, string>();
			Dictionary<string, string> bodyParams = new Dictionary<string, string>();

			switch (pushRequestContent.PushTarget) {
				case PushEnums.PushTarget.Device:
					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						queryString.Add("device_iden", pushRequestContent.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.Client:
					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						queryString.Add("client_iden", pushRequestContent.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.Email:
					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						queryString.Add("email", pushRequestContent.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.Channel:
					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushTargetValue)) {
						queryString.Add("channel_tag", pushRequestContent.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.All:
					queryString = null;
					break;
			}

			switch (pushRequestContent.PushType) {
				case PushEnums.PushType.Note:
					bodyParams.Add("type", "note");
					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushTitle)) {
						bodyParams.Add("title", pushRequestContent.PushTitle);
					}

					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushBody)) {
						bodyParams.Add("body", pushRequestContent.PushBody);
					}

					break;
				case PushEnums.PushType.Link:
					bodyParams.Add("type", "link");
					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushTitle)) {
						bodyParams.Add("title", pushRequestContent.PushTitle);
					}

					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushBody)) {
						bodyParams.Add("body", pushRequestContent.PushBody);
					}

					if (!Helpers.IsNullOrEmpty(pushRequestContent.LinkUrl)) {
						bodyParams.Add("url", pushRequestContent.LinkUrl);
					}

					break;
				case PushEnums.PushType.File:
					bodyParams.Add("type", "file");
					if (!Helpers.IsNullOrEmpty(pushRequestContent.FileName)) {
						bodyParams.Add("file_name", pushRequestContent.FileName);
					}

					if (!Helpers.IsNullOrEmpty(pushRequestContent.FileType)) {
						bodyParams.Add("file_type", pushRequestContent.FileType);
					}

					if (!Helpers.IsNullOrEmpty(pushRequestContent.FileUrl)) {
						bodyParams.Add("file_url", pushRequestContent.FileUrl);
					}

					if (!Helpers.IsNullOrEmpty(pushRequestContent.PushBody)) {
						bodyParams.Add("body", pushRequestContent.PushBody);
					}

					break;
				default:
					throw new InvalidRequestException();
			}

			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.POST, true, queryString, bodyParams);

			if (!requestStatus && Helpers.IsNullOrEmpty(response)) {
				return null;
			}

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Push notification send!", LogLevels.Trace);
			return DeserializeJsonObject<PushResponse>(response);
		}

		public ListSubscriptionsResponse GetSubscriptions() {
			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/subscriptions";
			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.GET);

			if (!requestStatus && Helpers.IsNullOrEmpty(response)) {
				return null;
			}

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched all subscriptions!");
			return DeserializeJsonObject<ListSubscriptionsResponse>(response);
		}

		public PushEnums.PushDeleteStatusCode DeletePush(string pushIdentifier) {
			if (Helpers.IsNullOrEmpty(pushIdentifier)) {
				throw new ParameterValueIsNullException("pushIdentifier");
			}

			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/subscriptions";
			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.DELETE);

			if (!requestStatus && Helpers.IsNullOrEmpty(response)) {
				return PushEnums.PushDeleteStatusCode.ObjectNotFound;
			}

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			PushEnums.PushDeleteStatusCode statusCode;

			try {
				DeletePushResponse pushResponse = JsonConvert.DeserializeObject<DeletePushResponse>(response);
				statusCode = pushResponse.ErrorReason.Message.Equals("Object not found", StringComparison.OrdinalIgnoreCase)
					? PushEnums.PushDeleteStatusCode.ObjectNotFound
					: PushEnums.PushDeleteStatusCode.Unknown;
			}
			catch (Exception) {
				Logger.Log("Deleted a push notification!");
				statusCode = PushEnums.PushDeleteStatusCode.Success;
			}

			return statusCode;
		}

		public PushListResponse GetAllPushes(PushListRequestContent listPushParams) {
			if (listPushParams == null) {
				throw new ParameterValueIsNullException("listPushParams is null.");
			}

			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/pushes";
			Dictionary<string, string> paramsValue = new Dictionary<string, string>();

			if (!Helpers.IsNullOrEmpty(listPushParams.Cursor)) {
				paramsValue.Add("cursor", listPushParams.Cursor);
			}

			if (!Helpers.IsNullOrEmpty(listPushParams.ModifiedAfter)) {
				paramsValue.Add("modified_after", listPushParams.ModifiedAfter);
			}

			if (listPushParams.MaxResults > 0) {
				paramsValue.Add("limit", listPushParams.MaxResults.ToString());
			}

			if (listPushParams.ActiveOnly) {
				paramsValue.Add("active", listPushParams.ActiveOnly.ToString().ToLower());
			}

			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.GET, false, paramsValue);

			if (!requestStatus && Helpers.IsNullOrEmpty(response)) {
				return null;
			}

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched all push notifications!");
			return DeserializeJsonObject<PushListResponse>(response);
		}

		public ChannelInfoResponse GetChannelInfo(string channelTag) {
			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			if (Helpers.IsNullOrEmpty(channelTag)) {
				throw new ParameterValueIsNullException("channelTag is null");
			}

			string requestUrl = "https://api.pushbullet.com/v2/channel-info";
			Dictionary<string, string> paramValues = new Dictionary<string, string> {
				{ "tag", channelTag }
			};

			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.GET, false, paramValues);

			if (!requestStatus && Helpers.IsNullOrEmpty(response)) {
				return null;
			}

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched channel information!");
			return DeserializeJsonObject<ChannelInfoResponse>(response);
		}

		private T DeserializeJsonObject<T>(string jsonObject) => Helpers.IsNullOrEmpty(jsonObject) ? throw new ResponseIsNullException() : JsonConvert.DeserializeObject<T>(jsonObject);

		private (bool, string) FetchApiResponse(string requestUrl, Method executionMethod = Method.GET, bool contentTypeUrlEncoded = false, Dictionary<string, string> queryParams = null, Dictionary<string, string> bodyContents = null) {
			if (Helpers.IsNullOrEmpty(requestUrl)) {
				Logger.Log("The specified request url is either null or empty.", LogLevels.Warn);
				return (false, null);
			}

			if (!Core.IsNetworkAvailable) {
				throw new RequestFailedException("No internet connectivity");
			}

			if (RequestInSleepMode) {
				return (false, null);
			}

			if (ApiFailedCount > 4) {
				RequestInSleepMode = true;
				Logger.Log("API requests have failed multiple times. Sleeping for 30 minutes...", LogLevels.Warn);
				Helpers.ScheduleTask(() => {
					RequestInSleepMode = false;
					ApiFailedCount = 0;
				}, TimeSpan.FromMinutes(30));
				return (false, null);
			}

			RestClient client = new RestClient(requestUrl);
			RestRequest request = new RestRequest(executionMethod);
			request.AddHeader("Access-Token", ClientAccessToken);

			if (contentTypeUrlEncoded) {
				request.AddHeader("content-type", "application/x-www-form-urlencoded");
			}

			if (queryParams != null && queryParams?.Count > 0) {
				foreach (KeyValuePair<string, string> param in queryParams) {
					if (!Helpers.IsNullOrEmpty(param.Key) && !Helpers.IsNullOrEmpty(param.Value)) {
						request.AddQueryParameter(param.Key, param.Value);
					}
				}
			}

			if (bodyContents != null && bodyContents?.Count > 0) {
				foreach (KeyValuePair<string, string> body in bodyContents) {
					if (!Helpers.IsNullOrEmpty(body.Key) && !Helpers.IsNullOrEmpty(body.Value)) {
						request.AddParameter(body.Key, body.Value, ParameterType.GetOrPost);
					}
				}
			}

			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to fetch. Status Code: " + response.StatusCode + "/" + response.ResponseStatus, LogLevels.Warn);
				ApiFailedCount++;
				return (false, response.Content);
			}

			string jsonResponse = response.Content;

			if (!Helpers.IsNullOrEmpty(jsonResponse)) {
				Logger.Log("Fetched json response.", LogLevels.Trace);
				return (true, jsonResponse);
			}

			return (false, jsonResponse);
		}
	}
}
