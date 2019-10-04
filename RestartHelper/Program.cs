using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RestartHelper {

	internal class Program {

		private static string? HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

		private static async Task Main(string[] args) {
			Console.WriteLine("Started restart helper...");
			Console.WriteLine("Assistant Directory: " + Directory.GetParent(HomeDirectory).Parent?.FullName + "/AssistantCore/");

			int delay = 100;

			if (args != null && args.Any()) {
				delay = Convert.ToInt32(args[0].Trim());
			}

			Console.WriteLine("Restarting in " + delay + " ms...");
			await Task.Delay(delay).ConfigureAwait(false);
			ExecuteBash("cd /home/pi/Desktop/HomeAssistant/AssistantCore && dotnet Assistant.dll", true);
			Console.WriteLine("Started Assistant...");
			Console.WriteLine("Exiting restarter.");
			await Task.Delay(1000).ConfigureAwait(false);
			Environment.Exit(0);
		}

		public static string ExecuteBash(string cmd, bool sudo) {
			if (cmd == null || string.IsNullOrWhiteSpace(cmd) || string.IsNullOrEmpty(cmd)) {
				return string.Empty;
			}

			string escapedArgs = cmd.Replace("\"", "\\\"");
			string args = $"-c \"{escapedArgs}\"";
			string argsWithSudo = $"-c \"sudo {escapedArgs}\"";

			using Process process = new Process() {
				StartInfo = new ProcessStartInfo {
					FileName = "/bin/bash",
					Arguments = sudo ? argsWithSudo : args,
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
