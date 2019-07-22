using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Newtonsoft.Json;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Geolocation {
	public class ZipCodeLocater {
		private Logger Logger { get; set; } = new Logger("ZIP-LOCATER");

		public ZipLocationResponse ZipLocationResponse { get; private set; } = new ZipLocationResponse();
		public ZipLocationResult ZipLocationResult { get; set; } = new ZipLocationResult();

		public static string GenerateRequestUrl(long pinCode) => pinCode > 0 ? Helpers.GetUrlToString($"http://www.postalpincode.in/api/pincode/{pinCode}", RestSharp.Method.GET) : null;
		public static string GenerateRequestUrl(string branchName) => Helpers.IsNullOrEmpty(branchName) ? null : Helpers.GetUrlToString($"http://www.postalpincode.in/api/pincode/{branchName}", RestSharp.Method.GET);

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

		private (bool status, ZipLocationResponse apiResult) FetchZipLocationApiResponse(long pinCode) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot continue as network isn't available.", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			if (pinCode <= 0) {
				Logger.Log("The specified pin code is incorrect.", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			string JSON = GenerateRequestUrl(pinCode);

			if (Helpers.IsNullOrEmpty(JSON)) {
				Logger.Log("Failed to fetch api response from api.postalpincode.in", LogLevels.Warn);
				return (false, ZipLocationResponse);
			}

			ZipLocationResponse = JsonConvert.DeserializeObject<ZipLocationResponse>(JSON);

			Logger.Log("Fetched postal zip code information successfully", LogLevels.Trace);
			return (true, ZipLocationResponse);
		}
	}
}
