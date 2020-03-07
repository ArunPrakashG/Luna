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

		public static int ShellIn_Integer(string? valueName) {
			if (string.IsNullOrEmpty(valueName)) {
				return -1;
			}

			bool inputCorrect = false;
			int count = 0;
			int maxTry = 3;
			
			do {
				if(count > maxTry) {
					Error("Execute the command again...");
					return -1;
				}

				if (count > 0)
					Info($">>> [Try: {count}] Enter input for value ' {valueName} ' ->");
				else
					Info($">>> Enter input for value ' {valueName} ' ->");

				Console.Beep();
				string? result = Console.ReadLine();

				if (string.IsNullOrEmpty(result) || !int.TryParse(result, out int responseVal)) {
					Error("Incorrect input. Try again!");
					count++;
					continue;
				}
				
				return responseVal;
			} while (!inputCorrect);

			return -1;
		}

		public static string? ShellIn_String(string? valueName) {
			if (string.IsNullOrEmpty(valueName)) {
				return null;
			}

			bool inputCorrect = false;
			int count = 0;
			int maxTry = 3;

			do {
				if (count > maxTry) {
					Error("Execute the command again...");
					return null;
				}

				if (count > 0)
					Info($">>> [Try: {count}] Enter input for value ' {valueName} ' ->");
				else
					Info($">>> Enter input for value ' {valueName} ' ->");

				Console.Beep();
				string? result = Console.ReadLine();

				if (string.IsNullOrEmpty(result)) {
					Error("Incorrect input. Try again!");
					count++;
					continue;
				}

				return result;
			} while (!inputCorrect);

			return null;
		}
	}
}
