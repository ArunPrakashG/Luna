using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Server.Authentication;
using Assistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Assistant.Server.Controllers {
	[Route("api/authenticate")]
	public class KestrelAuthentication : Controller {

		[HttpPost]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> Authenticate(AuthPostData postData) {
			if (Helpers.IsNullOrEmpty(postData.AuthToken)) {
				return BadRequest(new GenericResponse<string>("authToken cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(postData.ClientEmailId)) {
				return BadRequest(new GenericResponse<string>("emailId cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			AuthenticationClientData client = new AuthenticationClientData() {
				ClientAuthToken = postData.AuthToken,
				ClientEmailAddress = postData.ClientEmailId,
				AuthRequestTime = DateTime.Now
			};

			if (KestrelServer.Authentication.IsAllowedToExecute(postData)) {
				return Ok(new GenericResponse<string>("Client has been successfully authenticated.",
					Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			if (!KestrelServer.Authentication.AuthenticateClient(client)) {
				return BadRequest(new GenericResponse<string>("Failed to authenticate.", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (KestrelServer.AuthenticatedClients.Contains(client)) {
				return Ok(new GenericResponse<string>("Client has been successfully authenticated.",
					Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return BadRequest(new GenericResponse<string>("Failed to authenticate.", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
		}
	}
}
