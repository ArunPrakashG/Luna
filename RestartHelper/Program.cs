using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RestartHelper {

	internal class Program {

		private static string HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

		private static async Task Main(string[] args) {
			Console.WriteLine("Started restart helper...");
			Console.WriteLine("Assistant Directory: " + Directory.GetParent(HomeDirectory).Parent?.FullName + "/AssistantCore/");

			int Delay = 0;

			if (args != null && args.Any()) {
				Delay = Convert.ToInt32(args[0].Trim());
			}
			Console.WriteLine("Restarting in " + Delay + " ms...");
			await Task.Delay(Delay).ConfigureAwait(false);
			ExecuteBash("cd /home/pi/Desktop/HomeAssistant/AssistantCore && dotnet Assistant.dll");
			Console.WriteLine("Started Assistant...");
			Console.WriteLine("Exiting restarter.");
			Environment.Exit(0);
		}

		public static string ExecuteBash(string cmd) {
			if (cmd == null || string.IsNullOrEmpty(cmd) || string.IsNullOrWhiteSpace(cmd)) {
				return null;
			}

			string escapedArgs = cmd.Replace("\"", "\\\"");

			using Process process = new Process() {
				StartInfo = new ProcessStartInfo {
					FileName = "/bin/bash",
					Arguments = $"-c \"{escapedArgs}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};

			string result = string.Empty;

			if (process.Start()) {
				result = process.StandardOutput.ReadToEnd();
				process.WaitForExit(TimeSpan.FromMinutes(4).Milliseconds);
			}

			return result;
		}
	}
}
