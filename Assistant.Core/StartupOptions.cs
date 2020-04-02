using CommandLine;

namespace Assistant.Core {
	public class StartupOptions {
		[Option('d', "debug", Required = false, HelpText = "Displays all Trace level messages to console. (for debugging)")]
		public bool Debug { get; set; }

		[Option('s', "safe", Required = false, HelpText = "Enables safe mode so that only preconfigured pins can be modified.")]
		public bool Safe { get; set; }

		[Option("df", Required = false, HelpText = "Disable first chance exception logging when debug mode is enabled.")]
		public bool DisableFirstChance { get; set; }
	}
}
