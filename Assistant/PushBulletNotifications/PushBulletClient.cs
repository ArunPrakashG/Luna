using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.PushBulletNotifications.ApiResponse;
using Assistant.PushBulletNotifications.Exceptions;
using Assistant.PushBulletNotifications.Parameters;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using static Assistant.AssistantCore.Enums;
using static Assistant.PushBulletNotifications.PushEnums;

namespace Assistant.PushBulletNotifications {
	public class PushBulletClient {
		private readonly Logger Logger = new Logger("PUSH-BULLET-CLIENT");
		public string ClientAccessToken { get; private set; }
		public bool IsServiceLoaded { get; private set; }

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

		public UserDeviceList GetCurrentDevices() {
			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/devices";
			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.GET);

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched all devices!");
			return DeserializeJsonObject<UserDeviceList>(response);
		}

		public PushNote SendPush(PushMessageValues pushMessageValues) {
			if (pushMessageValues == null) {
				throw new ParameterValueIsNullException("pushMessageValues value is null.");
			}

			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/pushes";
			Dictionary<string, string> queryString = new Dictionary<string, string>();
			Dictionary<string, string> bodyParams = new Dictionary<string, string>();

			switch (pushMessageValues.PushTarget) {
				case PushTarget.Device:
					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushTargetValue)) {
						queryString.Add("device_iden", pushMessageValues.PushTargetValue);
					}

					break;
				case PushTarget.Client:
					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushTargetValue)) {
						queryString.Add("client_iden", pushMessageValues.PushTargetValue);
					}

					break;
				case PushTarget.Email:
					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushTargetValue)) {
						queryString.Add("email", pushMessageValues.PushTargetValue);
					}

					break;
				case PushTarget.Channel:
					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushTargetValue)) {
						queryString.Add("channel_tag", pushMessageValues.PushTargetValue);
					}

					break;
				case PushTarget.All:
					queryString = null;
					break;
			}

			switch (pushMessageValues.PushType) {
				case PushType.Note:
					bodyParams.Add("type", "note");
					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushTitle)) {
						bodyParams.Add("title", pushMessageValues.PushTitle);
					}

					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushBody)) {
						bodyParams.Add("body", pushMessageValues.PushBody);
					}

					break;
				case PushType.Link:
					bodyParams.Add("type", "link");
					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushTitle)) {
						bodyParams.Add("title", pushMessageValues.PushTitle);
					}

					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushBody)) {
						bodyParams.Add("body", pushMessageValues.PushBody);
					}

					if (!Helpers.IsNullOrEmpty(pushMessageValues.LinkUrl)) {
						bodyParams.Add("url", pushMessageValues.LinkUrl);
					}

					break;
				case PushType.File:
					bodyParams.Add("type", "file");
					if (!Helpers.IsNullOrEmpty(pushMessageValues.FileName)) {
						bodyParams.Add("file_name", pushMessageValues.FileName);
					}

					if (!Helpers.IsNullOrEmpty(pushMessageValues.FileType)) {
						bodyParams.Add("file_type", pushMessageValues.FileType);
					}

					if (!Helpers.IsNullOrEmpty(pushMessageValues.FileUrl)) {
						bodyParams.Add("file_url", pushMessageValues.FileUrl);
					}

					if (!Helpers.IsNullOrEmpty(pushMessageValues.PushBody)) {
						bodyParams.Add("body", pushMessageValues.PushBody);
					}

					break;
				default:
					throw new InvalidRequestException();
			}

			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.POST, true, queryString, bodyParams);

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Push notification send!", LogLevels.Trace);
			return DeserializeJsonObject<PushNote>(response);
		}

		public ListSubscriptions GetSubscriptions() {
			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/subscriptions";
			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.GET);

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched all subscriptions!");
			return DeserializeJsonObject<ListSubscriptions>(response);
		}

		public PushDeleteStatusCode DeletePush(string pushIdentifier) {
			if (Helpers.IsNullOrEmpty(pushIdentifier)) {
				throw new ParameterValueIsNullException("pushIdentifier");
			}

			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/subscriptions";
			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.DELETE);

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			PushDeleteStatusCode statusCode;

			try {
				DeletePush pushResponse = JsonConvert.DeserializeObject<DeletePush>(response);
				statusCode = pushResponse.ErrorReason.Message.Equals("Object not found", StringComparison.OrdinalIgnoreCase)
					? PushDeleteStatusCode.ObjectNotFound
					: PushDeleteStatusCode.Unknown;
			}
			catch (Exception) {
				Logger.Log("Deleted a push notification!");
				statusCode = PushDeleteStatusCode.Success;
			}

			return statusCode;
		}

		public ListPushes GetAllPushes(ListPushParams listPushParams) {
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

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched all push notifications!");
			return DeserializeJsonObject<ListPushes>(response);
		}

		public ChannelInfo GetChannelInfo(string channelTag) {
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

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			Logger.Log("Fetched channel information!");
			return DeserializeJsonObject<ChannelInfo>(response);
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
