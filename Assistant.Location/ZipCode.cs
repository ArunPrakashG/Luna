using Luna.Extensions;
using Luna.Logging;
using Luna.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Location {
	public class ZipCode {
		private ILogger Logger { get; set; } = new Logger(typeof(ZipCode).Name);
		private const int MAX_TRIES = 3;
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static readonly HttpClient Client = new HttpClient();

		public static string? GenerateRequestUrl(long pinCode) {
			if (pinCode <= 0) {
				return null;
			}

			return $"http://www.postalpincode.in/api/pincode/{pinCode}";
		}

		public static string? GenerateRequestUrl(string? branchName) {
			if (string.IsNullOrEmpty(branchName)) {
				return null;
			}

			return $"http://www.postalpincode.in/api/pincode/{branchName}";
		}

		public async Task<Response?> GetLocation(string? branchName) {
			if (string.IsNullOrEmpty(branchName)) {
				return null;
			}

			HttpContent? httpResponse = await Execute(branchName).ConfigureAwait(false);

			if (httpResponse == null) {
				return null;
			}

			try {
				string? responseContent = await httpResponse.ReadAsStringAsync().ConfigureAwait(false);

				if (string.IsNullOrEmpty(responseContent)) {
					return null;
				}

				return JsonConvert.DeserializeObject<Response>(responseContent);
			}
			catch (Exception e) {
				Logger.Exception(e);
				return null;
			}
		}

		public async Task<Response?> GetLocation(long pinCode) {
			if (pinCode <= 0) {
				return null;
			}

			HttpContent? httpResponse = await Execute(pinCode).ConfigureAwait(false);

			if (httpResponse == null) {
				return null;
			}

			try {
				string? responseContent = await httpResponse.ReadAsStringAsync().ConfigureAwait(false);

				if (string.IsNullOrEmpty(responseContent)) {
					return null;
				}

				return JsonConvert.DeserializeObject<Response>(responseContent);
			}
			catch (Exception e) {
				Logger.Exception(e);
				return null;
			}
		}

		private async Task<HttpContent?> Execute(object pinCode) {
			if (!Helpers.IsNetworkAvailable()) {
				Logger.Warning("Internet isn't available.");
				return null;
			}

			long pin = 0;
			string? branch = null;
			string? requestUrl = null;

			try {
				pin = (long) pinCode;
			}
			catch {
				branch = (string) pinCode;
			}

			if (!string.IsNullOrEmpty(branch)) {
				requestUrl = GenerateRequestUrl(branch);
			}
			else if (pin > 0) {
				requestUrl = GenerateRequestUrl(pin);
			}

			if (string.IsNullOrEmpty(requestUrl)) {
				Logger.Warning("URL is invalid or empty.");
				return null;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				for (int i = 0; i < MAX_TRIES; i++) {
					HttpResponseMessage resp = await Client.GetAsync(requestUrl).ConfigureAwait(false);

					if (resp == null || resp.StatusCode != System.Net.HttpStatusCode.OK || resp.Content == null) {
						Logger.Trace($"Request failed. {i}");
						continue;
					}

					return resp.Content;
				}

				Logger.Warning($"Zip location request failed after {MAX_TRIES} tries.");
				return null;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return null;
			}
			finally {
				Sync.Release();
			}
		}
	}
}
