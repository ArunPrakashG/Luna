using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Luna.Rest.Controllers {
	[Route("api/v1/assistant")]
	public class AssistantController : Controller {
		[HttpGet("status")]
		public ActionResult<RequestResponse> Status_GET([FromHeader] string authToken, [FromHeader] string localIp, [FromHeader] string publicIp) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_status", new RequestParameter(authToken, publicIp, localIp))));
		}

		[HttpPost]
		public ActionResult<RequestResponse> Shutdown_POST([FromHeader] string authToken, [FromHeader] string localIp, [FromHeader] string publicIp) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_shutdown", new RequestParameter(authToken, publicIp, localIp))));
		}

		[HttpPost]
		public ActionResult<RequestResponse> Exit_POST([FromHeader] string authToken, [FromHeader] string localIp, [FromHeader] string publicIp) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_exit", new RequestParameter(authToken, publicIp, localIp))));
		}
	}
}
