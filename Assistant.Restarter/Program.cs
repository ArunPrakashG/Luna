using Assistant.Extensions;
using System;
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
			Console.WriteLine("Started Assistant...");
			Console.WriteLine("cd /home/pi/Desktop/HomeAssistant/AssistantCore && dotnet Assistant.Core.dll".ExecuteBash(true));
			Console.WriteLine("Exiting restarter.");
			await Task.Delay(1000).ConfigureAwait(false);
			Environment.Exit(0);
		}
	}
}
