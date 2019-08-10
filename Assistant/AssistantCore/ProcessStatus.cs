
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

using ByteSizeLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.AssistantCore {

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
