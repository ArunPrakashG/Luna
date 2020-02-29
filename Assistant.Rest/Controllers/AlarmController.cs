using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.Rest.Controllers {
	[Route("api/v1/assistant/alarm")]
	public class AlarmController : Controller{
		[HttpPost]
		public ActionResult<RequestResponse> SetAlarm_POST(
			[FromHeader] string authToken,
			[FromHeader] string localIp,
			[FromHeader] string publicIp,
			[FromQuery] string alarmText,
			[FromQuery] DateTime alarmAt) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp) || string.IsNullOrEmpty(alarmText)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_alarm", new RequestParameter(authToken, publicIp, localIp, new object[] { alarmText, alarmAt }))));
		}

		[HttpDelete]
		public ActionResult<RequestResponse> RemoveAlarm_POST(
			[FromHeader] string authToken,
			[FromHeader] string localIp,
			[FromHeader] string publicIp,
			[FromQuery] string alarmUid) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp) || string.IsNullOrEmpty(alarmUid)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("assistant_alarm_delete", new RequestParameter(authToken, publicIp, localIp, new object[] { alarmUid }))));
		}
	}
}
