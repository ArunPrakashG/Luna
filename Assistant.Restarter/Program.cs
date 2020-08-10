using JsonCommandLine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Luna.External {
	internal partial class Program {
		private static string ExecutablePath;

		private static void Main(string[] args) {
			ParseStartupArguments();
		}

		private static void ParseStartupArguments() {
			Arguments args;
			using (CommandLineParser parser = new CommandLineParser(Environment.CommandLine)) {
				if (!parser.IsJsonType) {
					return;
				}

				ExecutablePath = parser.ExecutablePath ?? Assembly.GetExecutingAssembly().Location;
				args = parser.Parse() ?? new Arguments();
			}

			if (!args.ArgumentsExist) {
				return;
			}

			for (int i = 0; i < args.ArgumentCollection.Count; i++) {
				CommandLineArgument? arg = args.ArgumentCollection[i];

				switch (arg.BaseCommand) {
					case "update":
						HandleUpdateCommand(arg.Parameters);
						break;
					case "restart":
						HandleRestartCommand(arg.Parameters);
						break;
					case "cleanup":
						HandleCleaupCommand(arg.Parameters);
						break;
					default:
						break;
				}
			}
		}

		private static void HandleUpdateCommand(Dictionary<string, string> parameters) {
			bool presistLogs = true;
			string exePath = "";

			foreach (var p in parameters) {
				switch (p.Key) {
					case "presistLogs":
						presistLogs = bool.Parse(p.Value);
						break;
					case "exePath":
						exePath = p.Value;
						break;
				}
			}
		}

		private static void HandleRestartCommand(Dictionary<string, string> parameters) {
			bool forwardArgs = false;
			bool presistLogs = true;
			string exePath = "";
			Dictionary<string, string> forwardPairs = new Dictionary<string, string>();

			foreach (var p in parameters) {
				if (forwardArgs) {
					forwardPairs.Add(p.Key, p.Value);
					continue;
				}

				switch (p.Key) {
					case "presistLogs":
						presistLogs = bool.Parse(p.Value);
						break;
					case "forwardArgs":
						forwardArgs = bool.Parse(p.Value);
						break;
					case "exePath":
						exePath = p.Value;
						break;
				}
			}
		}

		private static void HandleCleaupCommand(Dictionary<string, string> parameters) {

		}
	}
}
