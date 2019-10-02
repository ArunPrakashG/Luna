using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Assistant.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unosquare.RaspberryIO;

namespace Assistant.Server.Responses {

	public class StatusResponse {
		[JsonProperty]
		public List<GpioPinConfig> GpioStatus { get; set; }

		[JsonProperty]
		public DateTime AssistantCurrentDateTime { get; set; }

		[JsonProperty]
		public string RaspberryPiUptime { get; set; }

		[JsonProperty]
		private string OsPlatform { get; set; }


		public StatusResponse GetResponse() {
			OsPlatform = Core.RunningPlatform.ToString();

			for (int i = 0; i <= Constants.BcmGpioPins.Length; i++) {
				if (!Core.CoreInitiationCompleted || Core.DisablePiMethods) {
					break;
				}

				GpioStatus.Add(Core.PiController.PinController.GetGpioConfig(i));
			}

			AssistantCurrentDateTime = DateTime.Now;
			RaspberryPiUptime = $"{Pi.Info.UptimeTimeSpan.TotalMinutes} minutes.";

			return this;
		}
	}
}
