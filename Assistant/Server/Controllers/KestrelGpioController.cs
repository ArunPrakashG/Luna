using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Server.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Unosquare.RaspberryIO.Abstractions;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Server.Controllers {

	[Route("api/gpio/status")]
	[Produces("application/json")]
	public class KestrelGpioController : Controller {

		[HttpGet]
		public ActionResult<GenericResponse<string>> GetAllPinStatus() {
			if (Tess.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch gpio status, Tess running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Tess.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			try {
				List<GPIOPinConfig> config = Tess.Controller.GPIOConfig;
				return Ok(new GenericResponse<List<GPIOPinConfig>>(config, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			catch (NullReferenceException) {
				return NotFound(new GenericResponse<string>("Failed to fetch pin status, possibly tess isn't fully started yet.", Enums.HttpStatusCodes.NoContent, DateTime.Now));
			}
		}

		[HttpGet("pin")]
		public ActionResult<GenericResponse<string>> GetPinStatus(int pinNumber) {
			if (pinNumber < 0 || pinNumber > 31) {
				return NotFound(Json(new GenericResponse<string>("The specified pin is either less than 0 or greater than 31.", Enums.HttpStatusCodes.NoContent, DateTime.Now)));
			}

			if (Tess.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin status, Tess running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Tess.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			try {
				GPIOPinConfig config = Tess.Controller.FetchPinStatus(pinNumber);
				return Ok(new GenericResponse<GPIOPinConfig>(config, Enums.HttpStatusCodes.OK, DateTime.Now));
			}
			catch (NullReferenceException) {
				return NotFound(new GenericResponse<string>("the specified pin isn't found or the pin configuration cannot be accessed", Enums.HttpStatusCodes.NotFound, DateTime.Now));
			}
		}

		[HttpGet("relay")]
		public ActionResult<GenericResponse<string>> GetRelayPinStatus() {
			if (Tess.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin mode, Tess running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Tess.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			List<GPIOPinConfig> resultConfig = new List<GPIOPinConfig>();

			foreach (int pin in Tess.Config.RelayPins) {
				resultConfig.Add(Tess.Controller.FetchPinStatus(pin));
			}

			if (resultConfig.Count > 0) {
				return Ok(
					new GenericResponse<List<GPIOPinConfig>>(resultConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return NotFound(new GenericResponse<string>(
				"Failed to fetch the pin status, either pin isn't valid or an unknown error occured.",
				Enums.HttpStatusCodes.NotFound, DateTime.Now));
		}

		[HttpGet("irsensor")]
		public ActionResult<GenericResponse<string>> GetIrSensorStatus() {
			if (Tess.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to fetch pin mode, Tess running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Tess.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			List<GPIOPinConfig> resultConfig = new List<GPIOPinConfig>();

			foreach (int pin in Tess.Config.IRSensorPins) {
				resultConfig.Add(Tess.Controller.FetchPinStatus(pin));
			}

			if (resultConfig.Count > 0) {
				return Ok(
					new GenericResponse<List<GPIOPinConfig>>(resultConfig, Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return NotFound(new GenericResponse<string>(
				"Failed to fetch the pin status, either pin isn't valid or an unknown error occured.",
				Enums.HttpStatusCodes.NotFound, DateTime.Now));
		}

	}

	[Route("api/gpio/config")]
	[Produces("application/json")]
	public class KestrelGpioConfigController : Controller {

		[HttpPost("pin")]
		public ActionResult<GenericResponse<string>> SetPinStatus(int pinNumber, PinMode pinMode, bool isOn) {
			if (pinNumber < 0 || pinNumber > 31) {
				return NotFound(new GenericResponse<string>("The specified pin is either less than 0 or greater than 31.", Enums.HttpStatusCodes.NoContent, DateTime.Now));
			}

			if (Tess.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to set pin mode, Tess running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Tess.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			bool result = Tess.Controller.SetGPIO(pinNumber, pinMode == PinMode.Output ? GpioPinDriveMode.Output : GpioPinDriveMode.Input,
				isOn ? GpioPinValue.Low : GpioPinValue.High);

			if (result) {
				return Ok(new GenericResponse<string>($"Successfully set {pinNumber} to {isOn} state. ({pinMode})",
					Enums.HttpStatusCodes.OK, DateTime.Now));
			}

			return BadRequest(new GenericResponse<string>("Failed to set the pin value.", Enums.HttpStatusCodes.BadRequest,
				DateTime.Now));
		}

		[HttpPost("relay")]
		public ActionResult<GenericResponse<string>> RelayCycle(GPIOCycles cycleMode) {
			if (Tess.IsUnknownOs) {
				return BadRequest(new GenericResponse<string>("Failed to set pin mode, Tess running on unknown OS.", Enums.HttpStatusCodes.BadRequest,
					DateTime.Now));
			}

			if (!Tess.CoreInitiationCompleted) {
				return BadRequest(new GenericResponse<string>(
					"TESS core initiation isn't completed yet, please be patient while it is completed. retry after 20 seconds.",
					HttpStatusCodes.BadRequest, DateTime.Now));
			}

			Helpers.InBackgroundThread(async () => await Tess.Controller.RelayTestService(cycleMode).ConfigureAwait(false), "Relay Cycle");
			return Ok(new GenericResponse<string>(
				$"Successfully started gpio relay test cycle. configured to {cycleMode.ToString()} cycle mode.", HttpStatusCodes.OK,
				DateTime.Now));
		}

	}
}
