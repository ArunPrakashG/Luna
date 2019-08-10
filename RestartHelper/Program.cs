
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
			ExecuteCommand("cd /home/pi/Desktop/HomeAssistant/AssistantCore && dotnet Assistant.dll");
			Console.WriteLine("Started Assistant...");
			Console.WriteLine("Exiting restarter.");
			Environment.Exit(0);
		}

		private static void ExecuteCommand(string command, bool redirectOutput = false) {
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
