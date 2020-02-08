using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.NLog;
using JetBrains.Annotations;
using Newtonsoft.Json;
using RestSharp;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Core.Geolocation {
	public class ZipCodeLocater {
		private Logger Logger { get; set; } = new Logger("ZIP-LOCATER");

		public ZipLocationResponse ZipLocationResponse { get; private set; } = new ZipLocationResponse();
		public ZipLocationResult ZipLocationResult { get; set; } = new ZipLocationResult();

		[CanBeNull]
		public static string? GenerateRequestUrl(long pinCode) {
			if (pinCode <= 0) {
				return string.Empty;
			}

			return Helpers.GetUrlToString($"http://www.postalpincode.in/api/pincode/{pinCode}", Method.GET);
		}

		[CanBeNull]
		public static string? GenerateRequestUrl(string branchName) {
			if (branchName.IsNull()) {
				return string.Empty;				
			}

			return Helpers.GetUrlToString($"http://www.postalpincode.in/api/pincode/{branchName}", Method.GET);
		}

		public (bool status, ZipLocationResult apiResult) GetZipLocationInfo(long pinCode) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot continue as network isn't available.", LogLevels.Warn);
				return (false, ZipLocationResult);
			}

			if (pinCode <= 0) {
				Logger.Log("The specified pin code is incorrect.", LogLevels.Warn);
				return (false, ZipLocationResult);
			}

			(bool status, ZipLocationResponse apiResult) = FetchZipLocationApiResponse(pinCode);

			if (status) {
				ZipLocationResult.Message = apiResult.Message;
				ZipLocationResult.Status = apiResult.Status;

				if(apiResult == null || apiResult.PostOffice == null) {
					return (false, ZipLocationResult);
				}

				foreach (ZipLocationResponse.Postoffice i in apiResult.PostOffice) {
					ZipLocationResult.PostOfficeCollection.Add((ZipLocationResult.PostOffice) i);
				}
				Logger.Log("Assigned the zip code values.", LogLevels.Trace);
				return (true, ZipLocationResult);
			}

			Logger.Log("Failed to assign the zip code values.", LogLevels.Trace);
			return (false, ZipLocationResult);
		}

		private (bool status, ZipLocationResponse apiResult) FetchZipLocationApiResponse(long pinCode) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot continue as network isn't available.", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			if (pinCode <= 0) {
				Logger.Log("The specified pin code is incorrect.", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			string? json = GenerateRequestUrl(pinCode);

			if (json == null || json.IsNull()) {
				Logger.Log("Failed to fetch api response from api.postalpincode.in", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			ZipLocationResponse = JsonConvert.DeserializeObject<ZipLocationResponse>(json);

			Logger.Log("Fetched postal zip code information successfully", LogLevels.Trace);
			return (true, ZipLocationResponse);
		}
	}
}
