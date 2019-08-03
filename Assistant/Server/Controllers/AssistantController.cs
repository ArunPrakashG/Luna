using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Assistant.Server.Authentication;

namespace Assistant.Server.Controllers {

	[Route("api/assistant/")]
	public class AssistantController : Controller {

		[HttpGet("status")]
		public ActionResult<GenericResponse<string>> DisplayAssistantStatus() {
			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			StatusResponse response = new StatusResponse();
			return Ok(new GenericResponse<StatusResponse>(response.GetResponse(), Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("exit")]
		public ActionResult<GenericResponse<string>> AssistantExit(AuthPostData auth, byte exitCode) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>(
					"Please provide the specified authentication information!",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.ScheduleTask(async () => { await Core.Exit(exitCode).ConfigureAwait(false); }, TimeSpan.FromSeconds(10));
			return Ok(new GenericResponse<string>($"Exiting {Core.AssistantName} in 10 seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("restart")]
		public ActionResult<GenericResponse<string>> AssistantRestart(AuthPostData auth, int delay) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>(
					"Please provide the specified authentication information!",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.InBackground(async () => await Core.Restart(delay).ConfigureAwait(false));
			return Ok(new GenericResponse<string>($"Restarting {Core.AssistantName} in {delay} seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("update")]
		public ActionResult<GenericResponse<string>> CheckAndUpdate(AuthPostData auth) {
			if (auth == null) {
				return BadRequest(new GenericResponse<string>(
					"Please provide the specified authentication information!",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(auth)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			(bool updateCheckStatus, Version updateVersion) updateResult = Core.Update.CheckAndUpdate(false);
			return Ok(new GenericResponse<string>(Core.Update.UpdateAvailable ? $"New update is available, Core will automatically update in 10 seconds." : $"Core is up-to-date! ({updateResult.updateVersion}/{Constants.Version})", Core.Update.UpdateAvailable ? $"Local version: {Constants.Version} / Latest version: {updateResult.updateVersion}" : "Update check success!", Enums.HttpStatusCodes.OK, DateTime.Now));
		}
	}
}
