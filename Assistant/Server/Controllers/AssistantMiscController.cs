
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
using Assistant.Geolocation;
using Assistant.Server.Responses;
using Assistant.Weather;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Assistant.Server.Controllers {
	[Route("api/misc/")]
	public class AssistantMiscController : Controller {
		[HttpPost("weather")]
		public ActionResult<GenericResponse<WeatherData>> GetWeatherInfo(string apiKey, int pinCode, string countryCode) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (pinCode <= 0) {
				return BadRequest(new GenericResponse<string>($"The specified pin code is invalid.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(countryCode)) {
				return BadRequest(new GenericResponse<string>($"The specified country code is invalid.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			(bool status, WeatherData response) = Core.WeatherApi.GetWeatherInfo(Core.Config.OpenWeatherApiKey, pinCode, countryCode);

			if (status) {
				return Ok(new GenericResponse<WeatherData>(response, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			else {
				return BadRequest(new GenericResponse<string>("Failed to fetch weather info.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}
		}

		[HttpPost("zip")]
		public ActionResult<GenericResponse<ZipLocationResult>> LocationFromZipCode(string apiKey, long zipCode) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (zipCode <= 0) {
				return BadRequest(new GenericResponse<string>($"The specified pin code is invalid.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			(bool status, ZipLocationResult apiResult) = Core.ZipCodeLocater.GetZipLocationInfo(zipCode);

			if (status) {
				return Ok(new GenericResponse<ZipLocationResult>(apiResult, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			else {
				return BadRequest(new GenericResponse<string>("Failed to fetch zip code info.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}
		}
	}
}
