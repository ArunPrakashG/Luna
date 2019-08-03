using Assistant.AssistantCore;
using Assistant.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Assistant.AssistantCore.PiGpio;
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
		private string OSCpuUsage { get; set; }

		[JsonProperty]
		private string OSRamUsage { get; set; }

		[JsonProperty]
		private string AssistantRamUsage { get; set; }

		[JsonProperty]
		private string OSPlatform { get; set; }


		public StatusResponse GetResponse() {
			if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.Windows)) {
				AssistantResourceUsage status = Core.AssistantStatus.GetProcessStatus();
				OSCpuUsage = status.TotalCpuUsage;
				OSRamUsage = status.TotalRamUsage;
				AssistantRamUsage = status.AssistantRamUsage;
				OSPlatform = "Windows";

				foreach (GpioPinConfig pin in Core.Controller.GpioConfigCollection) {
					if (pin.IsOn) {
						GpioStatus.Add(pin);
					}
				}

				AssistantCurrentDateTime = DateTime.Now;
				RaspberryPiUptime = $"{Pi.Info.UptimeTimeSpan.TotalMinutes} minutes.";

				return this;
			}
			else if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.Linux)) {
				OSCpuUsage = "Core is running on a Linux based platform, the required libraries are not supported.";
				OSRamUsage = "Core is running on a Linux based platform, the required libraries are not supported.";
				AssistantRamUsage = "Core is running on a Linux based platform, the required libraries are not supported.";
				OSPlatform = "Linux";
				foreach (GpioPinConfig pin in Core.Controller.GpioConfigCollection) {
					if (pin.IsOn) {
						GpioStatus.Add(pin);
					}
				}

				AssistantCurrentDateTime = DateTime.Now;
				RaspberryPiUptime = $"{Pi.Info.UptimeTimeSpan.TotalMinutes} minutes.";
				return this;
			}
			else if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.OSX)) {
				OSCpuUsage = "Core is running on a OSX based platform, the required libraries are not supported.";
				OSRamUsage = "Core is running on a OSX based platform, the required libraries are not supported.";
				AssistantRamUsage = "Core is running on a OSX based platform, the required libraries are not supported.";
				OSPlatform = "OSX";
				foreach (GpioPinConfig pin in Core.Controller.GpioConfigCollection) {
					if (pin.IsOn) {
						GpioStatus.Add(pin);
					}
				}

				AssistantCurrentDateTime = DateTime.Now;
				RaspberryPiUptime = $"{Pi.Info.UptimeTimeSpan.TotalMinutes} minutes.";
				return this;
			}
			else {
				OSCpuUsage = "Core is running on an Unknown platform, the required libraries won't run to prevent damage.";
				OSRamUsage = "Core is running on an Unknown platform, the required libraries won't run to prevent damage.";
				AssistantRamUsage = "Core is running on an Unknown platform, the required libraries won't run to prevent damage.";
				OSPlatform = "Unknown platform";
				foreach (GpioPinConfig pin in Core.Controller.GpioConfigCollection) {
					if (pin.IsOn) {
						GpioStatus.Add(pin);
					}
				}

				AssistantCurrentDateTime = DateTime.Now;
				RaspberryPiUptime = $"{Pi.Info.UptimeTimeSpan.TotalMinutes} minutes.";
				return this;
			}
		}
	}
}
