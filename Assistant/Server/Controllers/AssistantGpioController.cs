
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
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.Server.Controllers {

	[Route("api/gpio/status")]
	[Produces("application/json")]
	public class AssistantGpioController : Controller {

		[HttpGet]
		public ActionResult<GenericResponse<string>> GetAllPinStatus() {
			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch gpio status, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			try {
				List<GpioPinConfig> config = new List<GpioPinConfig>();
				for(int i=0; i<=40; i++) {
					config.Add(Core.Controller.GetPinConfig(i));
				}

				return Ok(new GenericResponse<List<GpioPinConfig>>(config, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			catch (NullReferenceException) {
				return NotFound(new GenericResponse<string>($"Failed to fetch pin status, possibly {Core.AssistantName} isn't fully started yet.", Enums.HttpStatusCodes.NoContent, DateTime.Now));
			}
		}

		[HttpGet("pin")]
		public ActionResult<GenericResponse<string>> GetPinStatus(int pinNumber) {
			if (pinNumber < 0 || pinNumber > 31) {
				return NotFound(Json(new GenericResponse<string>("The specified pin is either less than 0 or greater than 31.", Enums.HttpStatusCodes.NoContent, DateTime.Now)));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin status, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			try {
				GpioPinConfig config = Core.Controller.GetPinConfig(pinNumber);
				return Ok(new GenericResponse<GpioPinConfig>(config, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			catch (NullReferenceException) {
				return NotFound(new GenericResponse<string>("the specified pin isn't found or the pin configuration cannot be accessed", Enums.HttpStatusCodes.NotFound, DateTime.Now));
			}
		}

		[HttpGet("relay")]
		public ActionResult<GenericResponse<string>> GetRelayPinStatus() {
			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			List<GpioPinConfig> resultConfig = new List<GpioPinConfig>();

			foreach (int pin in Core.Config.OutputModePins) {
				resultConfig.Add(Core.Controller.GetPinConfig(pin));
			}

			if (resultConfig.Count > 0) {
				return Ok(
					new GenericResponse<List<GpioPinConfig>>(resultConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return NotFound(new GenericResponse<string>(
				"Failed to fetch the pin status, either pin isn't valid or an unknown error occured.",
				Enums.HttpStatusCodes.NotFound, DateTime.Now));
		}

		[HttpGet("irsensor")]
		public ActionResult<GenericResponse<string>> GetIrSensorStatus() {
			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			List<GpioPinConfig> resultConfig = new List<GpioPinConfig>();

			foreach (int pin in Core.Config.InputModePins) {
				resultConfig.Add(Core.Controller.GetPinConfig(pin));
			}

			if (resultConfig.Count > 0) {
				return Ok(
					new GenericResponse<List<GpioPinConfig>>(resultConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return NotFound(new GenericResponse<string>(
				"Failed to fetch the pin status, either pin isn't valid or an unknown error occured.",
				Enums.HttpStatusCodes.NotFound, DateTime.Now));
		}
	}

	[Route("api/gpio/config")]
	[Produces("application/json")]
	public class AssistantGpioConfigController : Controller {

		[HttpPost("pin")]
		public ActionResult<GenericResponse<string>> SetPinStatus(string apiKey, int pinNumber, Enums.PinMode pinMode, bool isOn) {
			if (pinNumber < 0 || pinNumber > 31) {
				return NotFound(new GenericResponse<string>("The specified pin is either less than 0 or greater than 31.", Enums.HttpStatusCodes.NoContent, DateTime.Now));
			}

			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to set pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			bool result = Core.Controller.SetGpioValue(pinNumber, pinMode == Enums.PinMode.Output ? GpioPinDriveMode.Output : GpioPinDriveMode.Input,
				isOn ? GpioPinValue.Low : GpioPinValue.High);

			if (result) {
				return Ok(new GenericResponse<string>($"Successfully set {pinNumber} to {isOn} state. ({pinMode})",
					Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return BadRequest(new GenericResponse<string>("Failed to set the pin value.", Enums.HttpStatusCodes.BadRequest,
				DateTime.Now));
		}

		[HttpPost("relay")]
		public ActionResult<GenericResponse<string>> RelayCycle(string apiKey, Enums.GPIOCycles cycleMode) {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return BadRequest(new GenericResponse<string>("Authentication code cannot be null, or empty.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (!KestrelServer.Authentication.IsAllowedToExecute(apiKey)) {
				return BadRequest(new GenericResponse<string>("You are not authenticated with the assistant. Please use the authentication endpoint to authenticate yourself!", Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			if (Core.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to set pin mode, Core running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Core.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					$"{Core.AssistantName} core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					Enums.HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.InBackgroundThread(async () => await Core.Controller.RelayTestService(cycleMode).ConfigureAwait(false), "Relay Cycle");
			return Ok(new GenericResponse<string>(
				$"Successfully started gpio relay test cycle. configured to {cycleMode.ToString()} cycle mode.", Enums.HttpStatusCodes.OK,
				DateTime.Now));
		}
	}
}
