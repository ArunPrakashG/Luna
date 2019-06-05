using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RestartHelper {

	internal class Program {

		public static string HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		private static async Task Main(string[] args) {
			Console.WriteLine("Started restart helper...");
			Console.WriteLine("TESS Directory: " + Directory.GetParent(HomeDirectory).Parent.FullName + "/AssistantCore/");

			int Delay = 0;

			if (args != null && args.Count() > 0) {
				Delay = Convert.ToInt32(args[0].Trim());
			}
			Console.WriteLine("Restarting in " + Delay + " ms...");
			await Task.Delay(Delay).ConfigureAwait(false);
			ExecuteCommand("cd /home/pi/Desktop/HomeAssistant && dotnet HomeAssistant.dll");
			Console.WriteLine("Started Assistant...");
			Console.WriteLine("Exiting restarter.");
			Environment.Exit(0);
		}

		public static void ExecuteCommand(string command, bool redirectOutput = false) {
			Process proc = new Process();
			proc.StartInfo.FileName = "/bin/bash";
			proc.StartInfo.Arguments = "-c \" " + command;

			if (redirectOutput) {
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
			}

			proc.Start();

			if (redirectOutput) {
				while (!proc.StandardOutput.EndOfStream) {
					Console.WriteLine(">>> " + proc.StandardOutput.ReadLine());
				}
			}
		}
	}
}
