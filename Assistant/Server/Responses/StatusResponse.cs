using AssistantCore;
using HomeAssistant.Extensions;
using Newtonsoft.Json;

namespace HomeAssistant.Server.Responses {
	public class StatusResponse {
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
				return this;
			}
			else if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.Linux)) {
				OSCpuUsage = "Core is running on a Linux based platform, the required libraries are not supported.";
				OSRamUsage = "Core is running on a Linux based platform, the required libraries are not supported.";
				AssistantRamUsage = "Core is running on a Linux based platform, the required libraries are not supported.";
				OSPlatform = "Linux";
				return this;
			}
			else if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.OSX)) {
				OSCpuUsage = "Core is running on a OSX based platform, the required libraries are not supported.";
				OSRamUsage = "Core is running on a OSX based platform, the required libraries are not supported.";
				AssistantRamUsage = "Core is running on a OSX based platform, the required libraries are not supported.";
				OSPlatform = "OSX";
				return this;
			}
			else {
				OSCpuUsage = "Core is running on an Unknown platform, the required libraries won't run to prevent damage.";
				OSRamUsage = "Core is running on an Unknown platform, the required libraries won't run to prevent damage.";
				AssistantRamUsage = "Core is running on an Unknown platform, the required libraries won't run to prevent damage.";
				OSPlatform = "Unknown platform";
				return this;
			}
		}
	}
}
