using ByteSizeLib;
using HomeAssistant.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HomeAssistant.Extensions;

namespace AssistantCore {
	public class AssistantResourceUsage {

		[JsonProperty]
		public string TotalCpuUsage { get; set; }
		[JsonProperty]
		public string TotalRamUsage { get; set; }

		[JsonProperty]
		public string AssistantRamUsage { get; set; }
	}

	public class ProcessStatus : IDisposable {
		private PerformanceCounter CpuCounter;
		private PerformanceCounter RamCounter;
		private readonly AssistantResourceUsage Usage = new AssistantResourceUsage();
		private readonly Logger Logger = new Logger("PROCESS-STATUS");

		public ProcessStatus() {
			InitialiseCPUCounter();
			InitializeRAMCounter();
		}

		public AssistantResourceUsage GetProcessStatus() {
			if (Helpers.GetOsPlatform().Equals(OSPlatform.Linux) || Helpers.GetOsPlatform().Equals(OSPlatform.OSX)) {
				throw new PlatformNotSupportedException("Current OS platform isn't supported to run this method. Try on Windows. (Linux/OSX)");
			}

			Usage.TotalCpuUsage = $"{CpuCounter.NextValue():##0} %";
			Usage.TotalRamUsage = $"{RamCounter.NextValue()} Mb";
			Usage.AssistantRamUsage = AssistantRamUsage();
			return Usage;
		}

		private string AssistantRamUsage() {
			double memorySize = ByteSize.FromBytes(Convert.ToDouble(Process.GetCurrentProcess().PrivateMemorySize64)).MegaBytes;
			return $"{memorySize} Mb";
		}

		private void InitialiseCPUCounter() {
			try {
				CpuCounter = new PerformanceCounter(
					"Processor",
					"% Processor Time",
					"_Total",
					true
				);
			}
			catch (Exception e) {
				Logger.Log(e);
				CpuCounter?.Dispose();
			}
		}

		private void InitializeRAMCounter() {
			try {
				RamCounter = new PerformanceCounter("Memory", "Available MBytes", true);
			}
			catch (Exception e) {
				Logger.Log(e);
				RamCounter?.Dispose();
			}
		}

		public void Dispose() {
			CpuCounter?.Dispose();
			RamCounter?.Dispose();
		}
	}
}
