using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Responses;
using Assistant.Weather;
using Microsoft.AspNetCore.Mvc;
using System;
using Assistant.Geolocation;

namespace Assistant.Server.Controllers {
	[Route("api/misc/")]
	public class KestrelMiscController : Controller {
		[HttpPost("weather")]
		public ActionResult<GenericResponse<string>> GetWeatherInfo([FromBody] int pinCode, [FromBody] string countryCode) {
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
		public ActionResult<GenericResponse<string>> LocationFromZipCode([FromBody] long zipCode) {
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
