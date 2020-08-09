using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Luna.External {
	public sealed class StartupArgument {
		[JsonProperty]
		public List<Argument> ArgumentCollection { get; set; }

		[JsonProperty]
		public bool ArgumentsExist => ArgumentCollection != null && ArgumentCollection.Count > 0;

		public StartupArgument(List<Argument> arguments) {
			ArgumentCollection = arguments;
		}

		public StartupArgument(params Argument[] arguments) {
			ArgumentCollection = arguments.ToList();
		}

		[JsonConstructor]
		public StartupArgument() {
			ArgumentCollection = new List<Argument>();
		}

		public string GetArgsObject() => JsonConvert.SerializeObject(this, Formatting.None).Replace('"', '\'');

		public static string? GetArgsObject(StartupArgument argumentBuilder) {
			if(argumentBuilder == null) {
				throw new NullReferenceException(nameof(argumentBuilder));
			}

			return argumentBuilder.GetArgsObject();
		}

		public static StartupArgument? Parse(string[] args) {
			if (args.Length <= 0 || string.IsNullOrEmpty(args[0])) {
				return null;
			}

			return JsonConvert.DeserializeObject<StartupArgument>(args[0].Replace('\'', '"'));
		}

		public static StartupArgument? Parse(string args) {
			if (string.IsNullOrEmpty(args)) {
				return null;
			}

			return JsonConvert.DeserializeObject<StartupArgument>(args.Replace('\'', '"'));
		}
	}
}
