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

namespace Luna.External {
	internal class Program {
		private static string? HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
		// update args =  -u --path "orginal_exe_path"
		// restart args = -r --path "orginal_exe_path"

		private static void Main(string[] args) {
			if(!string.IsNullOrEmpty(Environment.CommandLine)) {
				bool isJsonArgs = Environment.CommandLine.StartsWith('{') && args[0].EndsWith('}');
				StartupArgument? argument = StartupArgument.Parse(Environment.CommandLine);
			}

			Argument arg1 = new Argument("update", new Dictionary<string, string>() {
				{"path", Environment.CurrentDirectory }
			});

			Argument arg2 = new Argument("restart", new Dictionary<string, string>() {
				{"path", Environment.CurrentDirectory },
				{"debug", "Enabled" }
			});

			StartupArgument arguments = new StartupArgument(arg1, arg2);
			string argumentJson = arguments.AsArgs();
			Console.WriteLine(argumentJson);
		}

		public class StartupArgument {
			[JsonProperty]
			public List<Argument> ArgumentCollection { get; set; }

			public StartupArgument(List<Argument> arguments) {
				ArgumentCollection = arguments;
			}

			public StartupArgument(params Argument[] arguments) {
				ArgumentCollection = arguments.ToList();
			}

			[JsonConstructor]
			public StartupArgument() { }

			public string AsArgs() => JsonConvert.SerializeObject(this, Formatting.None);

			public static StartupArgument? Parse(string[] args) {
				if(args.Length <= 0 || string.IsNullOrEmpty(args[0])) {
					return null;
				}

				return JsonConvert.DeserializeObject<StartupArgument>(args[0]);
			}

			public static StartupArgument? Parse(string args) {
				if (string.IsNullOrEmpty(args)) {
					return null;
				}

				return JsonConvert.DeserializeObject<StartupArgument>(args);
			}
		}

		public class Argument {
			[JsonProperty]
			public string BaseCommand { get; set; }

			[JsonProperty]
			public Dictionary<string, string> Parameters { get; set; }

			public Argument(string baseCommand, Dictionary<string, string> parameters) {
				BaseCommand = baseCommand;
				Parameters = parameters;
			}

			[JsonConstructor]
			public Argument() { }
		}
	}
}
