
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

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
