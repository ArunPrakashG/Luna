using System;

namespace Assistant.Extensions.Shared.Shell {
	public static class ShellOut {
		public static void Info(string? msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.WriteLine(msg);
		}

		public static void Error(string? msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(msg);
			Console.ResetColor();
		}

		public static void Exception(Exception e) {
			if (e == null) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(e);
			Console.ResetColor();
		}
	}
}
