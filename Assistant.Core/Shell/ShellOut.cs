using System;

namespace Assistant.Core.Shell {
	internal static class ShellOut {
		internal static void Info(string? msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.WriteLine(msg);
		}

		internal static void Error(string? msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(msg);
			Console.ResetColor();
		}

		internal static void Exception(Exception e) {
			if (e == null) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(e);
			Console.ResetColor();
		}
	}
}
