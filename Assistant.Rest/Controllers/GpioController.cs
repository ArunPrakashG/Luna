using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Luna.Rest.Controllers {
	[Route("api/v1/gpio")]
	public class GpioController : Controller {
		[HttpGet("status")]
		public ActionResult<RequestResponse> Status_GET([FromHeader] string authToken, [FromHeader] string localIp, [FromHeader] string publicIp) {
			if(string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp)) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("gpio_status", new RequestParameter(authToken, publicIp, localIp))));
		}

		[HttpPost("pin")]
		public ActionResult<RequestResponse> SetPin_POST(
			[FromHeader] string authToken,
			[FromHeader] string localIp,
			[FromHeader] string publicIp,
			[FromQuery] int gpioPin,
			[FromQuery] RestCore.GpioPinMode pinMode,
			[FromQuery] RestCore.GpioPinState pinState) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp) || gpioPin <= 0) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("set_gpio", new RequestParameter(authToken, publicIp, localIp, new object[] { gpioPin, pinMode, pinState}))));
		}

		[HttpPost("pin")]
		public ActionResult<RequestResponse> SetPinDelayed_POST(
			[FromHeader] string authToken,
			[FromHeader] string localIp,
			[FromHeader] string publicIp,
			[FromQuery] int gpioPin,
			[FromQuery] RestCore.GpioPinMode pinMode,
			[FromQuery] RestCore.GpioPinState pinState,
			[FromQuery] int delay) {
			if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(localIp) || string.IsNullOrEmpty(publicIp) || gpioPin <= 0 || delay <= 0) {
				return BadRequest();
			}

			return Ok(Json(RestCore.GetResponse("set_gpio_delay", new RequestParameter(authToken, publicIp, localIp, new object[] { gpioPin, pinMode, pinState, delay }))));
		}
	}
}
