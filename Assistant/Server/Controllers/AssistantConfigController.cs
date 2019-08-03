using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Server.Authentication;
using Assistant.Server.Responses;

namespace Assistant.Server.Controllers {

	[Route("api/config")]
	public class AssistantConfigController : Controller {

		[HttpGet("coreconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetCoreConfig(AuthPostData auth) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<CoreConfig>(Core.Config, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpGet("gpioconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetGpioConfig(AuthPostData auth) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
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

			return Ok(new GenericResponse<List<GpioPinConfig>>(Core.Controller.GpioConfigCollection, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("coreconfig")]
		[Consumes("application/json")]
		public ActionResult<GenericResponse<string>> SetCoreConfig(AuthPostData auth, [FromBody]CoreConfig config) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
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
		public ActionResult<GenericResponse<string>> SetGpioConfig(AuthPostData auth, [FromBody] GpioConfigRoot config) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be equal to 0, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
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

			if (config == null) {
				return BadRequest(new GenericResponse<string>("Config cant be empty.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (config.Equals(Core.Controller.GpioConfigRoot)) {
				return BadRequest(new GenericResponse<string>(
					"The new config and the current config is already same. update is unnecessary",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<GpioConfigRoot>(Core.GPIOConfigHandler.SaveGPIOConfig(config),
				$"Config updated, please wait a while for {Core.AssistantName} to update the core values.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}
	}
}
