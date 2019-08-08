using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Assistant.Server.Controllers {
	[Route("api/core/")]
	public class SystemController : Controller {

		[HttpPost("restart")]
		public ActionResult<GenericResponse<string>> RestartSystem(string apiKey) {
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

			Helpers.ScheduleTask(async () => await Core.SystemRestart().ConfigureAwait(false), TimeSpan.FromSeconds(2));
			return Ok(new GenericResponse<string>("Restarting system in 2 seconds...", Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("shutdown")]
		public ActionResult<GenericResponse<string>> ShutdownSystem(string apiKey) {
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

			Helpers.ScheduleTask(async () => await Core.SystemShutdown().ConfigureAwait(false), TimeSpan.FromSeconds(2));
			return Ok(new GenericResponse<string>("Shutting down system in 2 seconds...", Enums.HttpStatusCodes.OK, DateTime.Now));
		}
	}
}
