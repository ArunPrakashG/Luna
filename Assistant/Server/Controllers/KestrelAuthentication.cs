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
		public ActionResult<GenericResponse<string>> Authenticate(string userName, string userDevice, string authToken, string emailId) {
			if (Helpers.IsNullOrEmpty(userName)) {
				return BadRequest(new GenericResponse<string>("userName cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(userDevice)) {
				return BadRequest(new GenericResponse<string>("userDevice cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(authToken)) {
				return BadRequest(new GenericResponse<string>("authToken cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(emailId)) {
				return BadRequest(new GenericResponse<string>("emailId cannot be null!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			AuthenticationClientData client = new AuthenticationClientData() {
				ClientAuthToken = authToken,
				ClientUserName = userName,
				ClientDevice = userDevice,
				ClientEmailAddress = emailId,
				AuthRequestTime = DateTime.Now
			};

			if (!KestrelServer.Authentication.IsClientAuthenticated(client)) {
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
