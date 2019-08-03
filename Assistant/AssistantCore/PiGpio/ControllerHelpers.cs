using System;
using System.Collections.Generic;
using System.Text;
using Assistant.Log;
using Unosquare.RaspberryIO;

namespace Assistant.AssistantCore.PiGpio {
	public static class ControllerHelpers {

		private static readonly Logger Logger = new Logger("PI-HELPERS");

		public static void DisplayPiInfo() {
			Logger.Log($"OS: {Pi.Info.OperatingSystem.SysName}", Enums.LogLevels.Trace);
			Logger.Log($"Processor count: {Pi.Info.ProcessorCount}", Enums.LogLevels.Trace);
			Logger.Log($"Model name: {Pi.Info.ModelName}", Enums.LogLevels.Trace);
			Logger.Log($"Release name: {Pi.Info.OperatingSystem.Release}", Enums.LogLevels.Trace);
			Logger.Log($"Board revision: {Pi.Info.BoardRevision}", Enums.LogLevels.Trace);
			Logger.Log($"Pi Version: {Pi.Info.RaspberryPiVersion.ToString()}", Enums.LogLevels.Trace);
			Logger.Log($"Memory size: {Pi.Info.MemorySize.ToString()}", Enums.LogLevels.Trace);
			Logger.Log($"Serial: {Pi.Info.Serial}", Enums.LogLevels.Trace);
			Logger.Log($"Pi Uptime: {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 4)} minutes", Enums.LogLevels.Trace);
		}
	}
}
