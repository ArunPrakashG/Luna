using Assistant.PushBulletNotifications.ApiResponse;
using Assistant.PushBulletNotifications.Exceptions;
using Assistant.PushBulletNotifications.Parameters;
using HomeAssistant.AssistantCore;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using static HomeAssistant.AssistantCore.Enums;

namespace Assistant.PushBulletNotifications {
	public class PushBulletClient {
		private Logger Logger = new Logger("PUSH-BULLET-CLIENT");
		public string ClientAccessToken { get; private set; }

		public PushBulletClient(string apiKey) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				throw new IncorrectAccessTokenException(apiKey);
			}

			ClientAccessToken = apiKey;
		}

		public PushBulletClient() {
			if (Helpers.IsNullOrEmpty(Core.Config.PushBulletApiKey)) {
				throw new IncorrectAccessTokenException(Core.Config.PushBulletApiKey);
			}

			ClientAccessToken = Core.Config.PushBulletApiKey;
		}

		public ListDevice GetCurrentDevices() {
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

			return DeserializeJsonObject<ListDevice>(response);
		}

		public PushNote SendPush(SendPushParams sendPushParams) {
			if (sendPushParams == null) {
				throw new ArgumentNullException("Push request parameter is null.");
			}

			if (Helpers.IsNullOrEmpty(ClientAccessToken)) {
				throw new IncorrectAccessTokenException(ClientAccessToken);
			}

			string requestUrl = "https://api.pushbullet.com/v2/pushes";
			var queryString = new Dictionary<string, string>();
			var bodyParams = new Dictionary<string, string>();

			switch (sendPushParams.PushTarget) {
				case PushEnums.PushTarget.Device:					
					if (!Helpers.IsNullOrEmpty(sendPushParams.PushTargetValue)) {
						queryString.Add("device_iden", sendPushParams.PushTargetValue);
					}
					
					break;
				case PushEnums.PushTarget.Client:
					if (!Helpers.IsNullOrEmpty(sendPushParams.PushTargetValue)) {
						queryString.Add("client_iden", sendPushParams.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.Email:
					if (!Helpers.IsNullOrEmpty(sendPushParams.PushTargetValue)) {
						queryString.Add("email", sendPushParams.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.Channel:
					if (!Helpers.IsNullOrEmpty(sendPushParams.PushTargetValue)) {
						queryString.Add("channel_tag", sendPushParams.PushTargetValue);
					}

					break;
				case PushEnums.PushTarget.All:
					queryString = null;
					break;
			}

			switch (sendPushParams.PushType) {
				case PushEnums.PushType.Note:
					bodyParams.Add("type", "note");
					if (!Helpers.IsNullOrEmpty(sendPushParams.PushTitle)) {
						bodyParams.Add("title", sendPushParams.PushTitle);
					}

					if (!Helpers.IsNullOrEmpty(sendPushParams.PushBody)) {
						bodyParams.Add("body", sendPushParams.PushBody);
					}

					break;
				case PushEnums.PushType.Link:
					bodyParams.Add("type", "link");
					if (!Helpers.IsNullOrEmpty(sendPushParams.PushTitle)) {
						bodyParams.Add("title", sendPushParams.PushTitle);
					}

					if (!Helpers.IsNullOrEmpty(sendPushParams.PushBody)) {
						bodyParams.Add("body", sendPushParams.PushBody);
					}

					if (!Helpers.IsNullOrEmpty(sendPushParams.LinkUrl)) {
						bodyParams.Add("url", sendPushParams.LinkUrl);
					}

					break;
				case PushEnums.PushType.File:
					bodyParams.Add("type", "file");
					if (!Helpers.IsNullOrEmpty(sendPushParams.FileName)) {
						bodyParams.Add("file_name", sendPushParams.FileName);
					}

					if (!Helpers.IsNullOrEmpty(sendPushParams.FileType)) {
						bodyParams.Add("file_type", sendPushParams.FileType);
					}

					if (!Helpers.IsNullOrEmpty(sendPushParams.FileUrl)) {
						bodyParams.Add("file_url ", sendPushParams.FileUrl);
					}

					if (!Helpers.IsNullOrEmpty(sendPushParams.PushBody)) {
						bodyParams.Add("body ", sendPushParams.PushBody);
					}

					break;
				default:
					throw new InvalidRequestException();
			}

			(bool requestStatus, string response) = FetchApiResponse(requestUrl, Method.POST, queryString, bodyParams);

			if (!requestStatus) {
				throw new RequestFailedException();
			}

			if (Helpers.IsNullOrEmpty(response)) {
				throw new ResponseIsNullException();
			}

			return DeserializeJsonObject<PushNote>(response);
		}

		private T DeserializeJsonObject<T>(string jsonObject) => Helpers.IsNullOrEmpty(jsonObject) ? throw new ResponseIsNullException() : JsonConvert.DeserializeObject<T>(jsonObject);

		private (bool, string) FetchApiResponse(string requestUrl, Method executionMethod = Method.GET, Dictionary<string, string> queryParams = null, Dictionary<string, string> bodyContents = null) {
			if (Helpers.IsNullOrEmpty(requestUrl)) {
				Logger.Log("The specified request url is either null or empty.", LogLevels.Warn);
				return (false, null);
			}

			RestClient client = new RestClient(requestUrl);
			RestRequest request = new RestRequest(executionMethod);
			request.AddHeader("Access-Token", ClientAccessToken);
			request.AddHeader("Content-Type", "application/json");
			if (queryParams != null || queryParams.Count > 0) {
				foreach (KeyValuePair<string, string> param in queryParams) {
					if (!Helpers.IsNullOrEmpty(param.Key) && !Helpers.IsNullOrEmpty(param.Value)) {
						request.AddQueryParameter(param.Key, param.Value);
					}
				}
			}

			if(bodyContents != null && bodyContents.Count > 0) {
				foreach (KeyValuePair<string, string> body in bodyContents) {
					if (!Helpers.IsNullOrEmpty(body.Key) && !Helpers.IsNullOrEmpty(body.Value)) {
						request.AddParameter(body.Key, body.Value, ParameterType.RequestBody);
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
