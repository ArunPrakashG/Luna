using HomeAssistant.Log;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HomeAssistant.Extensions {

	internal static class OS {
		internal static bool IsUnix => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
		private static Logger Logger = new Logger("OS");

		internal static void Init(bool systemRequired) {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				DisableQuickEditMode();

				if (systemRequired) {
					KeepWindowsSystemActive();
				}
			}
		}

		internal static void UnixSetFileAccessExecutable(string path) {
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
				Logger.Log(nameof(path));
				return;
			}

			// Chmod() returns 0 on success, -1 on failure
			if (NativeMethods.Chmod(path, (int) NativeMethods.UnixExecutePermission) != 0) {
				Logger.Log(string.Format("Failed due to error: {0}", Marshal.GetLastWin32Error()));
			}
		}

		private static void DisableQuickEditMode() {
			if (Console.IsOutputRedirected) {
				return;
			}

			// http://stackoverflow.com/questions/30418886/how-and-why-does-quickedit-mode-in-command-prompt-freeze-applications
			IntPtr consoleHandle = NativeMethods.GetStdHandle(NativeMethods.StandardInputHandle);

			if (!NativeMethods.GetConsoleMode(consoleHandle, out uint consoleMode)) {
				Logger.Log("Failed!");
				return;
			}

			consoleMode &= ~NativeMethods.EnableQuickEditMode;

			if (!NativeMethods.SetConsoleMode(consoleHandle, consoleMode)) {
				Logger.Log("Failed!");
			}
		}

		private static void KeepWindowsSystemActive() {
			// This function calls unmanaged API in order to tell Windows OS that it should not enter sleep state while the program is running
			// If user wishes to enter sleep mode, then he should use ShutdownOnFarmingFinished or manage ASF process with third-party tool or script
			// More info: https://msdn.microsoft.com/library/windows/desktop/aa373208(v=vs.85).aspx
			NativeMethods.EExecutionState result = NativeMethods.SetThreadExecutionState(NativeMethods.AwakeExecutionState);

			// SetThreadExecutionState() returns NULL on failure, which is mapped to 0 (EExecutionState.Error) in our case
			if (result == NativeMethods.EExecutionState.Error) {
				Logger.Log(string.Format("Failed due to error: {0}", result));
			}
		}

		private static class NativeMethods {
			internal const EExecutionState AwakeExecutionState = EExecutionState.SystemRequired | EExecutionState.AwayModeRequired | EExecutionState.Continuous;
			internal const uint EnableQuickEditMode = 0x0040;
			internal const sbyte StandardInputHandle = -10;
			internal const EUnixPermission UnixExecutePermission = EUnixPermission.UserRead | EUnixPermission.UserWrite | EUnixPermission.UserExecute | EUnixPermission.GroupRead | EUnixPermission.GroupExecute | EUnixPermission.OtherRead | EUnixPermission.OtherExecute;

			[DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
			internal static extern int Chmod(string path, int mode);

			[DllImport("kernel32.dll")]
			internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

			[DllImport("kernel32.dll")]
			internal static extern IntPtr GetStdHandle(int nStdHandle);

			[DllImport("kernel32.dll")]
			internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

			[DllImport("kernel32.dll")]
			internal static extern EExecutionState SetThreadExecutionState(EExecutionState executionState);

			[Flags]
			internal enum EExecutionState : uint {
				Error = 0,
				SystemRequired = 0x00000001,
				AwayModeRequired = 0x00000040,
				Continuous = 0x80000000
			}

			[Flags]
			internal enum EUnixPermission : ushort {
				OtherExecute = 0x1,
				OtherRead = 0x4,
				GroupExecute = 0x8,
				GroupRead = 0x20,
				UserExecute = 0x40,
				UserWrite = 0x80,
				UserRead = 0x100

				/*
				OtherWrite = 0x2
				GroupWrite = 0x10
				*/
			}
		}
	}
}
