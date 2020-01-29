using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Servers.Kestrel.Responses;
using Assistant.Speech;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Assistant.Servers.Kestrel.Controllers {

	[Route("api/speech/")]
	public class AssistantSpeechController : Controller {

		//TODO: make endpoint for speech communication between two apps
		[HttpPost("")]
		public ActionResult<GenericResponse<string>> PostSpeechResult(string speechText) {
			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(speechText)) {
				return BadRequest(new GenericResponse<string>("The speech cannot be null!",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			SpeechBus.RecognizedSpeech = speechText;
			return Ok(new GenericResponse<string>("Success!", Enums.HttpStatusCodes.OK, DateTime.Now));
		}
	}
}
