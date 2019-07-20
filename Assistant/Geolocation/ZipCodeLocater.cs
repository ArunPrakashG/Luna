using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using static HomeAssistant.AssistantCore.Enums;

namespace Assistant.Geolocation {
	public class ZipCodeLocater {
		private Logger Logger { get; set; } = new Logger("ZIP-LOCATER");

		public ZipLocationResponse.Rootobject ZipLocationResponse { get; private set; } = new ZipLocationResponse.Rootobject();
		public ZipLocationResult ZipLocationResult { get; set; } = new ZipLocationResult();

		public static string GenerateRequestUrl(long pinCode) => pinCode > 0 ? Helpers.GetUrlToString($"http://www.postalpincode.in/api/pincode/{pinCode}", RestSharp.Method.GET) : null;
		public static string GenerateRequestUrl(string branchName) => Helpers.IsNullOrEmpty(branchName) ? null : Helpers.GetUrlToString($"http://www.postalpincode.in/api/pincode/{branchName}", RestSharp.Method.GET);

		public (bool status, ZipLocationResult apiResult) GetZipLocationInfo(long pinCode) {
			if (pinCode <= 0) {
				Logger.Log("The specified pin code is incorrect.", LogLevels.Warn);
				return (false, ZipLocationResult);
			}

			(bool status, ZipLocationResponse.Rootobject apiResult) = FetchZipLocationApiResponse(pinCode);

			if (status) {
				ZipLocationResult.Message = apiResult.Message;
				ZipLocationResult.Status = apiResult.Status;
				foreach (ZipLocationResponse.Postoffice i in apiResult.PostOffice) {
					ZipLocationResult.PostOfficeCollection.Add((ZipLocationResult.PostOffice) i);
				}
				Logger.Log("Assigned the zip code values.", LogLevels.Trace);
				return (true, ZipLocationResult);
			}
			else {
				Logger.Log("Failed to assign the zip code values.", LogLevels.Trace);
				return (false, ZipLocationResult);
			}
		}

		private (bool status, ZipLocationResponse.Rootobject apiResult) FetchZipLocationApiResponse(long pinCode) {
			if (pinCode <= 0) {
				Logger.Log("The specified pin code is incorrect.", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			string JSON = GenerateRequestUrl(pinCode);

			if (Helpers.IsNullOrEmpty(JSON)) {
				Logger.Log("Failed to fetch api response from api.postalpincode.in", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			ZipLocationResponse = JsonConvert.DeserializeObject<ZipLocationResponse.Rootobject>(JSON);

			Logger.Log("Fetched postal zip code information successfully", LogLevels.Trace);
			return (true, ZipLocationResponse);
		}
	}
}
