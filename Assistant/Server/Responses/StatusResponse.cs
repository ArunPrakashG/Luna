using HomeAssistant.Core;
using HomeAssistant.Extensions;
using Newtonsoft.Json;

namespace HomeAssistant.Server.Responses {
	public class StatusResponse {
		[JsonProperty]
		private string OSCpuUsage { get; set; }
		[JsonProperty]
		private string OSRamUsage { get; set; }
		[JsonProperty]
		private string TessRamUsage { get; set; }
		[JsonProperty]
		private string OSPlatform { get; set; }

		public StatusResponse GetResponse() {
			if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.Windows)) {
				AssistantResourceUsage status = Tess.AssistantStatus.GetProcessStatus();
				OSCpuUsage = status.TotalCpuUsage;
				OSRamUsage = status.TotalRamUsage;
				TessRamUsage = status.TessRamUsage;
				OSPlatform = "Windows";
				return this;
			}
			else if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.Linux)) {
				OSCpuUsage = "Tess is running on a Linux based platform, the required libraries are not supported.";
				OSRamUsage = "Tess is running on a Linux based platform, the required libraries are not supported.";
				TessRamUsage = "Tess is running on a Linux based platform, the required libraries are not supported.";
				OSPlatform = "Linux";
				return this;
			}
			else if (Helpers.GetOsPlatform().Equals(System.Runtime.InteropServices.OSPlatform.OSX)) {
				OSCpuUsage = "Tess is running on a OSX based platform, the required libraries are not supported.";
				OSRamUsage = "Tess is running on a OSX based platform, the required libraries are not supported.";
				TessRamUsage = "Tess is running on a OSX based platform, the required libraries are not supported.";
				OSPlatform = "OSX";
				return this;
			}
			else {
				OSCpuUsage = "Tess is running on an Unknown platform, the required libraries won't run to prevent damage.";
				OSRamUsage = "Tess is running on an Unknown platform, the required libraries won't run to prevent damage.";
				TessRamUsage = "Tess is running on an Unknown platform, the required libraries won't run to prevent damage.";
				OSPlatform = "Unknown platform";
				return this;
			}
		}
	}
}
