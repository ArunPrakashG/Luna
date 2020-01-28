using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Assistant.Logging {
	internal class Helpers {
		private static string FileSeperator { get; set; } = @"\";

		internal static string GetFileName(string? path) {
			if (string.IsNullOrEmpty(path)) {
				return string.Empty;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return Path.GetFileName(path) ?? string.Empty;

			return path.Substring(path.LastIndexOf(FileSeperator, StringComparison.Ordinal) + 1);
		}
	}
}
