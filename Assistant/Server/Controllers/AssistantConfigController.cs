using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Assistant.Server.Controllers {

	[Route("api/config")]
	public class AssistantConfigController : Controller {

		[HttpGet("coreconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetCoreConfig(string apiKey) {
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

			return Ok(new GenericResponse<CoreConfig>(Core.Config, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("coreconfig")]
		[Consumes("application/json")]
		public ActionResult<GenericResponse<string>> SetCoreConfig(string apiKey, [FromBody]CoreConfig config) {
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

			if (config.Equals(Core.Config)) {
				return BadRequest(new GenericResponse<string>(
					"The new config and the current config is already same. update is unnecessary",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<CoreConfig>(Core.Config.SaveConfig(config),
				$"Config updated, please wait a while for {Core.AssistantName} to update the core values.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}
	}
}
