using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.External {
	internal static class Logger {
		internal static void Info(string? msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($"{DateTime.Now} | INFO | {msg}");
			Console.ResetColor();
		}

		internal static void Error(string msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"{DateTime.Now} | ERROR | {msg}");
			Console.ResetColor();
		}

		internal static void Warn(string msg) {
			if (string.IsNullOrEmpty(msg)) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine($"{DateTime.Now} | WARN | {msg}");
			Console.ResetColor();
		}
	}
}
