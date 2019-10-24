using AssistantSharedLibrary.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssistantSharedLibrary {
	public static class Helpers {
		public static OSPlatform GetOsPlatform() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				return OSPlatform.Windows;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				return OSPlatform.Linux;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				return OSPlatform.OSX;
			}

			return OSPlatform.Linux;
		}

		public static Timer ScheduleTask(Action action, TimeSpan delay) {
			if (action == null) {
				EventLogger.LogWarning("Action is null! " + nameof(action));
				return null;
			}

			Timer TaskSchedulerTimer = null;

			TaskSchedulerTimer = new Timer(e => {
				InBackgroundThread(action, delay.TotalMilliseconds.ToString());

				TaskSchedulerTimer?.Dispose();
			}, null, delay, delay);

			return TaskSchedulerTimer;
		}

		public static bool IsSocketConnected(Socket s) {
			if (s == null) {
				return false;
			}

			bool part1 = s.Poll(1000, SelectMode.SelectRead);
			bool part2 = s.Available == 0;
			if (part1 && part2) {
				return false;
			}

			return true;
		}

		public static string ExecuteBash(this string cmd, bool sudo) {
			if (string.IsNullOrEmpty(cmd)) {
				return string.Empty;
			}

			string escapedArgs = cmd.Replace("\"", "\\\"");
			string args = $"-c \"{escapedArgs}\"";
			string argsWithSudo = $"-c \"sudo {escapedArgs}\"";

			Process process = new Process() {
				StartInfo = new ProcessStartInfo {
					FileName = "/bin/bash",
					Arguments = sudo ? argsWithSudo : args,
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};

			string result = string.Empty;

			if (process.Start()) {
				result = process.StandardOutput.ReadToEnd();
				process.WaitForExit(TimeSpan.FromMinutes(4).Milliseconds);
			}

			process?.Dispose();
			return result;
		}

		public static string GetLocalIpAddress() {
			string localIP = string.Empty;

			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				if (endPoint != null) {
					localIP = endPoint.Address.ToString();
				}
			}

			return localIP;
		}

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}

		public static string GetExternalIp(string requestUrl = "https://api.ipify.org/") {
			WebClient client = new WebClient();
			string result = client.DownloadString(requestUrl).Trim('\n');
			client?.Dispose();
			return result;
		}

		public static DateTime ConvertTo24Hours(DateTime source) =>
			DateTime.TryParse(source.ToString("yyyy MMMM d HH:mm:ss tt"), out DateTime result) ? result : DateTime.Now;

		public static DateTime ConvertTo12Hours(DateTime source) =>
			DateTime.TryParse(source.ToString("dddd, dd MMMM yyyy"), out DateTime result) ? result : DateTime.Now;

		public static (int, Thread) InBackgroundThread(Action action, string threadName, bool longRunning = false) {
			if (action == null) {
				EventLogger.LogWarning("Action is null! " + nameof(action));
				return (0, null);
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning) {
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = threadName;
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
			return (BackgroundThread.ManagedThreadId, BackgroundThread);
		}

		public static void InBackground(Action action, bool longRunning = false) {
			if (action == null) {
				EventLogger.LogWarning("Action is null! " + nameof(action));
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static void InBackground<T>(Func<T> function, bool longRunning = false) {
			if (function == null) {
				EventLogger.LogWarning("Function is null! " + nameof(function));
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(function, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static async Task<IList<T>> InParallel<T>(IEnumerable<Task<T>> tasks) {
			if (tasks == null) {
				EventLogger.LogWarning(nameof(tasks));
				return null;
			}

			IList<T> results = await Task.WhenAll(tasks).ConfigureAwait(false);
			return results;
		}

		public static async Task InParallel(IEnumerable<Task> tasks) {
			if (tasks == null) {
				EventLogger.LogWarning(nameof(tasks));
				return;
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		public static bool CheckForInternetConnection() {			
			try {
				Ping myPing = new Ping();
				string host = "8.8.8.8";
				byte[] buffer = new byte[32];
				int timeout = 1000;
				PingOptions pingOptions = new PingOptions();
				PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);				
				return reply != null && reply.Status == IPStatus.Success;
			}
			catch (Exception e) {
				EventLogger.LogException(e);
				return false;
			}
		}


	}
}
