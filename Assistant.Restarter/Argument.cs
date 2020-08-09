using Newtonsoft.Json;
using System.Collections.Generic;

namespace Luna.External {
	public sealed class Argument {
		[JsonProperty]
		public string BaseCommand { get; set; }

		[JsonProperty]
		public Dictionary<string, string> Parameters { get; set; }

		[JsonProperty]
		public int ParameterCount => Parameters != null ? Parameters.Count : 0;

		public Argument(string baseCommand, Dictionary<string, string> parameters) {
			BaseCommand = baseCommand;
			Parameters = parameters;
		}

		[JsonConstructor]
		public Argument() {
			BaseCommand = string.Empty;
			Parameters = new Dictionary<string, string>();
		}
	}
}
