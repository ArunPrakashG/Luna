
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Assistant.Server.Authentication;
using Assistant.Server.Responses;

namespace Assistant.Server.Controllers {

	[Route("api/config")]
	public class AssistantConfigController : Controller {

		[HttpGet("coreconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetCoreConfig(string apiKey) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<CoreConfig>(Core.Config, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpGet("gpioconfig")]
		[Produces("application/json")]
		public ActionResult<GenericResponse<string>> GetGpioConfig(string apiKey) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch gpio config, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<List<GpioPinConfig>>(Core.Controller.GpioConfigCollection, Enums.HttpStatusCodes.OK, DateTime.Now));
		}

		[HttpPost("coreconfig")]
		[Consumes("application/json")]
		public ActionResult<GenericResponse<string>> SetCoreConfig(string apiKey, [FromBody]CoreConfig config) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (config == null) {
				return BadRequest(new GenericResponse<string>("Config cant be empty.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (config.Equals(Core.Config)) {
				return BadRequest(new GenericResponse<string>(
					"The new config and the current config is already same. update is unnecessary",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<CoreConfig>(Core.Config.SaveConfig(config),
				$"Config updated, please wait a while for {Core.AssistantName} to update the core values.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}

		[HttpPost("gpioconfig")]
		[Consumes("application/json")]
		public ActionResult<GenericResponse<string>> SetGpioConfig(string apiKey, [FromBody] GpioConfigRoot config) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to update gpio config, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (config == null) {
				return BadRequest(new GenericResponse<string>("Config cant be empty.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (config.Equals(Core.Controller.GpioConfigRoot)) {
				return BadRequest(new GenericResponse<string>(
					"The new config and the current config is already same. update is unnecessary",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			return Ok(new GenericResponse<GpioConfigRoot>(Core.GPIOConfigHandler.SaveGPIOConfig(config),
				$"Config updated, please wait a while for {Core.AssistantName} to update the core values.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}
	}
}
