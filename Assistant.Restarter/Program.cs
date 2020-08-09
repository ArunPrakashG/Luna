using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JsonCommandLine;

namespace Luna.External {
	internal partial class Program {
		private static string? HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
		// update args =  -u --path "orginal_exe_path"
		// restart args = -r --path "orginal_exe_path"

		private static void Main(string[] args) {
			args = ParseStartupArgs(Environment.CommandLine);
			using(CommandLineParser parser = new CommandLineParser(Environment.CommandLine)) {

			}

			if (args.Length > 0) {
				bool isJsonArgs = args[1].StartsWith('{') && args[1].EndsWith('}');
				StartupArgument? argument = StartupArgument.Parse(args[1]);
			}

			Argument arg1 = new Argument("update", new Dictionary<string, string>() {
				{"path", Environment.CurrentDirectory }
			});

			Argument arg2 = new Argument("restart", new Dictionary<string, string>() {
				{"path", Environment.CurrentDirectory },
				{"debug", "Enabled" }
			});

			StartupArgument arguments = new StartupArgument(arg1, arg2);
			string argumentJson = arguments.GetArgsObject();
			Console.WriteLine(argumentJson);
		}

		private static string[] ParseStartupArgs(string commandLine) {
			if (string.IsNullOrEmpty(commandLine)) {
				return Array.Empty<string>();
			}

			return commandLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
