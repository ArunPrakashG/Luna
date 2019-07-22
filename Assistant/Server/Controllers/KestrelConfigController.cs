using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Responses;

namespace Assistant.Server.Controllers {

	[Route("api/config")]
	public class KestrelConfigController : Controller {

		[HttpGet("coreconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetCoreConfig(int authCode) {
			if (authCode == 0) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (authCode != Constants.KestrelAuthCode) {
				return BadRequest(new GenericResponse<string>("Authentication code is incorrect, you are not allowed to execute this command.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<CoreConfig>(Core.Config, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpGet("gpioconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetGpioConfig(int authCode) {
			if (authCode == 0) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch gpio config, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (authCode != Constants.KestrelAuthCode) {
				return BadRequest(new GenericResponse<string>("Authentication code is incorrect, you are not allowed to execute this command.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<List<GPIOPinConfig>>(Core.Controller.GPIOConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("coreconfig")]
		[Consumes("application/json")]
		public ActionResult<GenericResponse<string>> SetCoreConfig(int authCode, [FromBody]CoreConfig config) {
			if (authCode == 0) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (authCode != Constants.KestrelAuthCode) {
				return BadRequest(new GenericResponse<string>("Authentication code is incorrect, you are not allowed to execute this command.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (config == null) {
				return BadRequest(new GenericResponse<string>("Config cant be empty.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (config.Equals(Core.Config)) {
				return BadRequest(new GenericResponse<string>(
					"The new config and the current config is already same. update is unnecessary",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<CoreConfig>(Core.Config.SaveConfig(config),
				$"Config updated, please wait a while for {Core.AssistantName} to update the core values.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("gpioconfig")]
		[Consumes("application/json")]
		public ActionResult<GenericResponse<string>> SetGpioConfig(int authCode, [FromBody] GPIOConfigRoot config) {
			if (authCode == 0) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to update gpio config, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (authCode != Constants.KestrelAuthCode) {
				return BadRequest(new GenericResponse<string>("Authentication code is incorrect, you are not allowed to execute this command.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (config == null) {
				return BadRequest(new GenericResponse<string>("Config cant be empty.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (config.Equals(Core.Controller.GPIOConfigRoot)) {
				return BadRequest(new GenericResponse<string>(
					"The new config and the current config is already same. update is unnecessary",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<GPIOConfigRoot>(Core.GPIOConfigHandler.SaveGPIOConfig(config),
				$"Config updated, please wait a while for {Core.AssistantName} to update the core values.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}
	}
}
