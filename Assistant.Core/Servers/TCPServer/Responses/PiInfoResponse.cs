using Assistant.AssistantCore;
using Newtonsoft.Json;
using System;
using Unosquare.RaspberryIO;

namespace Assistant.Servers.TCPServer.Responses {
	public class PiInfoResponse {
		[JsonProperty]
		public string OperatingSystemName { get; set; } = string.Empty;

		[JsonProperty]
		public int ProcessorCount { get; set; }

		[JsonProperty]
		public string CpuModelName { get; set; } = string.Empty;

		[JsonProperty]
		public string RaspberryPiVersion { get; set; } = string.Empty;

		[JsonProperty]
		public int BoardRevision { get; set; }

		[JsonProperty]
		public string MemorySize { get; set; } = string.Empty;

		[JsonProperty]
		public string SerialNumber { get; set; } = string.Empty;

		[JsonProperty]
		public double UptimeMinutes { get; set; }

		public string? GetPiInfo() {
			if (Core.DisablePiMethods || !Core.CoreInitiationCompleted) {
				return null;
			}

			OperatingSystemName = Pi.Info.OperatingSystem.SysName;
			ProcessorCount = Pi.Info.ProcessorCount;
			CpuModelName = Pi.Info.ModelName;
			RaspberryPiVersion = Pi.Info.RaspberryPiVersion.ToString();
			BoardRevision = Pi.Info.BoardRevision;
			MemorySize = Pi.Info.MemorySize.ToString();
			SerialNumber = Pi.Info.Serial;
			UptimeMinutes = Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 4);

			return JsonConvert.SerializeObject(this);
		}
	}
}
