using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Luna.Rest.Controllers {
	[Route("api/v1/assistant/remainder")]
	public class RemainderController : Controller {
		[HttpPost]
		public ActionResult<RequestResponse> SetRemainder_POST(
			[FromHeader] string authToken,
			[FromHeader] string localIp,
			[FromHeader] string publicIp,
			[FromQuery] string remainderText,
			[FromQuery] DateTime remainderAt) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp) || string.IsNullOrEmpty(remainderText)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_remainder", new RequestParameter(authToken, publicIp, localIp, new object[] { remainderText, remainderAt }))));
		}

		[HttpDelete]
		public ActionResult<RequestResponse> SetRemainder_POST(
			[FromHeader] string authToken,
			[FromHeader] string localIp,
			[FromHeader] string publicIp,
			[FromQuery] string remainderUid) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp) || string.IsNullOrEmpty(remainderUid)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_remainder_delete", new RequestParameter(authToken, publicIp, localIp, new object[] { remainderUid }))));
		}
	}
}
