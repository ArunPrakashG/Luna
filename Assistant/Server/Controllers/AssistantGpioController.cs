using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Server.Controllers {

	[Route("api/gpio/status")]
	[Produces("application/json")]
	public class AssistantGpioController : Controller {

		[HttpGet]
		public ActionResult<GenericResponse<string>> GetAllPinStatus() {
			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch gpio status, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			try {
				return Ok(new GenericResponse<List<GpioPinConfig>>(PiController.PinConfigCollection, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			catch (NullReferenceException) {
				return NotFound(new GenericResponse<string>($"Failed to fetch pin status, possibly {Core.AssistantName} isn't fully started yet.", Enums.HttpStatusCodes.NoContent, DateTime.Now));
			}
		}

		[HttpGet("pin")]
		public ActionResult<GenericResponse<string>> GetPinStatus(int pinNumber) {
			if (pinNumber < 0 || pinNumber > 31) {
				return NotFound(Json(new GenericResponse<string>("The specified pin is either less than 0 or greater than 31.", Enums.HttpStatusCodes.NoContent, DateTime.Now)));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin status, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			try {
				if(Core.PiController == null) {
					return BadRequest(new GenericResponse<string>("Internal error. the pi controller malfunctioned.", HttpStatusCodes.BadRequest, DateTime.Now));
				}

				GpioPinConfig config = Core.PiController.GetPinController().GetGpioConfig(pinNumber);
				return Ok(new GenericResponse<GpioPinConfig>(config, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			catch (NullReferenceException) {
				return NotFound(new GenericResponse<string>("the specified pin isn't found or the pin configuration cannot be accessed", Enums.HttpStatusCodes.NotFound, DateTime.Now));
			}
		}

		[HttpGet("relay")]
		public ActionResult<GenericResponse<string>> GetRelayPinStatus() {
			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			List<GpioPinConfig> resultConfig = new List<GpioPinConfig>();

			if (Core.PiController == null) {
				return BadRequest(new GenericResponse<string>("Internal error. the pi controller malfunctioned.", HttpStatusCodes.BadRequest, DateTime.Now));
			}

			foreach (int pin in Core.Config.RelayPins) {
				resultConfig.Add(Core.PiController.GetPinController().GetGpioConfig(pin));
			}

			if (resultConfig.Count > 0) {
				return Ok(
					new GenericResponse<List<GpioPinConfig>>(resultConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return NotFound(new GenericResponse<string>(
				"Failed to fetch the pin status, either pin isn't valid or an unknown error occured.",
				Enums.HttpStatusCodes.NotFound, DateTime.Now));
		}

		[HttpGet("irsensor")]
		public ActionResult<GenericResponse<string>> GetIrSensorStatus() {
			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			List<GpioPinConfig> resultConfig = new List<GpioPinConfig>();

			if (Core.PiController == null) {
				return BadRequest(new GenericResponse<string>("Internal error. the pi controller malfunctioned.", HttpStatusCodes.BadRequest, DateTime.Now));
			}

			foreach (int pin in Core.Config.IRSensorPins) {
				resultConfig.Add(Core.PiController.GetPinController().GetGpioConfig(pin));
			}

			if (resultConfig.Count > 0) {
				return Ok(
					new GenericResponse<List<GpioPinConfig>>(resultConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return NotFound(new GenericResponse<string>(
				"Failed to fetch the pin status, either pin isn't valid or an unknown error occured.",
				Enums.HttpStatusCodes.NotFound, DateTime.Now));
		}
	}

	[Route("api/gpio/config")]
	[Produces("application/json")]
	public class AssistantGpioConfigController : Controller {

		[HttpPost("pin")]
		public ActionResult<GenericResponse<string>> SetPinStatus(string apiKey, int pinNumber, GpioPinMode pinMode, bool isOn) {
			if (pinNumber < 0 || pinNumber > 31) {
				return NotFound(new GenericResponse<string>("The specified pin is either less than 0 or greater than 31.", Enums.HttpStatusCodes.NoContent, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to set pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.PiController == null) {
				return BadRequest(new GenericResponse<string>("Internal error. the pi controller malfunctioned.", HttpStatusCodes.BadRequest, DateTime.Now));
			}

			bool result = Core.PiController.GetPinController().SetGpioValue(pinNumber, pinMode, isOn ? Enums.GpioPinState.On : Enums.GpioPinState.Off);

			if (result) {
				return Ok(new GenericResponse<string>($"Successfully set {pinNumber} to {isOn} state. ({pinMode})",
					Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return BadRequest(new GenericResponse<string>("Failed to set the pin value.", Enums.HttpStatusCodes.BadRequest,
				DateTime.Now));
		}

		[HttpPost("relay")]
		public ActionResult<GenericResponse<string>> RelayCycle(string apiKey, Enums.GpioCycles cycleMode) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to set pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.PiController == null) {
				return BadRequest(new GenericResponse<string>("Internal error. the pi controller malfunctioned.", HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.InBackgroundThread(async () => await Core.PiController.GetPinController().RelayTestServiceAsync(cycleMode).ConfigureAwait(false), "Relay Cycle");
			return Ok(new GenericResponse<string>(
				$"Successfully started gpio relay test cycle. configured to {cycleMode.ToString()} cycle mode.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}
	}
}
