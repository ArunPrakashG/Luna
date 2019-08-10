
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
