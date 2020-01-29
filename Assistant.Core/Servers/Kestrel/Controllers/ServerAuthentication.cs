using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Servers.Kestrel.Responses;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Assistant.Servers.Kestrel.Controllers {
	[Route("api/authenticate")]
	public class ServerAuthentication : Controller {

		[HttpPost]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> Authenticate(string apiKey) {
			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("apiKey cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return Ok(new GenericResponse<string>("Client has been successfully authenticated.",
					Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return BadRequest(new GenericResponse<string>("Failed to authenticate.", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
		}
	}
}
