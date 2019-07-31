using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Assistant.Server.Controllers {

	[Route("api/assistant/")]
	public class AssistantMemory : Controller {

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
		public ActionResult<GenericResponse<string>> AssistantExit(byte exitCode) {
			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.ScheduleTask(async () => { await Core.Exit(exitCode).ConfigureAwait(false); }, TimeSpan.FromSeconds(10));
			return Ok(new GenericResponse<string>($"Exiting {Core.AssistantName} in 10 seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("restart")]
		public ActionResult<GenericResponse<string>> AssistantExit(int delay) {
			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.InBackground(async () => await Core.Restart(delay).ConfigureAwait(false));
			return Ok(new GenericResponse<string>($"Restarting {Core.AssistantName} in {delay} seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("update")]
		public ActionResult<GenericResponse<string>> CheckAndUpdate() {
			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			(bool updateCheckStatus, Version updateVersion) updateResult = Core.Update.CheckAndUpdate(false);
			return Ok(new GenericResponse<string>(Core.Update.UpdateAvailable ? $"New update is available, Core will automatically update in 10 seconds." : $"Core is up-to-date! ({updateResult.updateVersion}/{Constants.Version})", Core.Update.UpdateAvailable ? $"Local version: {Constants.Version} / Latest version: {updateResult.updateVersion}" : "Update check success!", Enums.HttpStatusCodes.OK, DateTime.Now));
		}
	}
}
