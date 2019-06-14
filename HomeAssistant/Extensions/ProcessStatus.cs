using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HomeAssistant.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HomeAssistant.Extensions {
    public class TessUsage {

        [JsonProperty]
        public string CpuUsage { get; set; }
        [JsonProperty]
        public string RamUsage { get; set; }
	}

	public class ProcessStatus : IDisposable {
		private PerformanceCounter CpuCounter;
		private PerformanceCounter RamCounter;
		private TessUsage Usage = new TessUsage();
		private Logger Logger = new Logger("PROCESS-STATUS");

		public ProcessStatus() {
			InitialiseCPUCounter();
			InitializeRAMCounter();
		}

		public TessUsage GetProcessStatus() {
			Usage.CpuUsage = $"{CpuCounter.NextValue():##0} %";
			Usage.RamUsage = $"{RamCounter.NextValue()} MB";
			return Usage;
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
