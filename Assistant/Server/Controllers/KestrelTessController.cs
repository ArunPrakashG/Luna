using HomeAssistant.AssistantCore;
using HomeAssistant.Extensions;
using HomeAssistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static HomeAssistant.AssistantCore.Enums;

namespace HomeAssistant.Server.Controllers {

	[Route("api/tess/")]
	public class TessMemory : Controller {

		[HttpGet("status")]
		public ActionResult<GenericResponse<string>> GetMemoryUsage() {
			if (!AssistantCore.Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			StatusResponse response = new StatusResponse();
			return Ok(new GenericResponse<StatusResponse>(response.GetResponse(), Helpers.GetOsPlatform() != OSPlatform.Windows ? "Libraries used for checking status are Windows platform dependent." : "Fetched status successfully!", HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("exit")]
		public ActionResult<GenericResponse<string>> ExitTess(byte exitCode) {
			if (!AssistantCore.Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.ScheduleTask(async () => { await AssistantCore.Core.Exit(exitCode).ConfigureAwait(false); }, TimeSpan.FromSeconds(10));
			return Ok(new GenericResponse<string>("Exiting tess in 10 seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("restart")]
		public ActionResult<GenericResponse<string>> ExitTess(int delay) {
			if (!AssistantCore.Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.InBackground(async () => await AssistantCore.Core.Restart(delay).ConfigureAwait(false));
			return Ok(new GenericResponse<string>($"Restarting tess in {delay} seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("update")]
		public ActionResult<GenericResponse<string>> CheckAndUpdate() {
			if (!AssistantCore.Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			(bool updateCheckStatus, Version updateVersion) updateResult = AssistantCore.Core.Update.CheckAndUpdate(false);
			return Ok(new GenericResponse<string>(AssistantCore.Core.Update.UpdateAvailable ? $"New update is available, Core will automatically update in 10 seconds." : $"Core is up-to-date! ({updateResult.updateVersion}/{Constants.Version})", AssistantCore.Core.Update.UpdateAvailable ? $"Local version: {Constants.Version} / Latest version: {updateResult.updateVersion}" : "Update check success!", Enums.HttpStatusCodes.OK, DateTime.Now));
		}
	}
}
