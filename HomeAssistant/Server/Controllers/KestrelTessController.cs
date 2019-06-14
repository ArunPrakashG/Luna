using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace HomeAssistant.Server.Controllers {

	[Route("api/tess/")]
	public class TessMemory : Controller {

		[HttpGet("status")]
		public ActionResult<GenericResponse<string>> GetMemoryUsage() {
			if (Tess.IsUnknownOs) {
				TessUsage status = Tess.TessStatus.GetProcessStatus();
				return Ok(new GenericResponse<TessUsage>(status, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			else {
				return Conflict(new GenericResponse<string>("Platform isn't supported for this api endpoint.",
					Enums.HttpStatusCodes.Conflict, DateTime.Now));
			}
		}

		[HttpPost("exit/{exitCode}")]
		public ActionResult<GenericResponse<string>> ExitTess(byte exitCode) {
			Helpers.ScheduleTask(async () => { await Tess.Exit(exitCode).ConfigureAwait(false); }, TimeSpan.FromSeconds(10));
			return Ok(new GenericResponse<string>("Exiting tess in 10 seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("restart/{delay}")]
		public ActionResult<GenericResponse<string>> ExitTess(int delay) {
			Helpers.InBackground(async () => await Tess.Restart(delay).ConfigureAwait(false));
			return Ok(new GenericResponse<string>($"Restarting tess in {delay} seconds...", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("update")]
		public ActionResult<GenericResponse<string>> CheckAndUpdate() {
			Helpers.InBackground(async () => await Tess.Update.CheckAndUpdate(false).ConfigureAwait(false));
			return Ok(new GenericResponse<string>("Checking for update...", Enums.HttpStatusCodes.OK, DateTime.Now));
		}
	}
}
