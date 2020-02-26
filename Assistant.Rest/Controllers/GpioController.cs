using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.Rest.Controllers {
	// TODO: Define object which stores various function pointers which will take in parameters and return response
	//		 return that response as request response.
	//		 Implement Function event system in such a way.
	[Route("api/v1/gpio")]
	public class GpioController : Controller {
		[HttpGet("status")]
		public ActionResult<string> Status() {

		}
	}
}
