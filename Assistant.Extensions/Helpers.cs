using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Figgle;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Extensions
{
	public static class Helpers
	{
		private static readonly ILogger Logger = new Logger("HELPERS");

		private static string FileSeperator { get; set; } = @"\";

		public static void SetFileSeperator() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				FileSeperator = "//";
				Logger.Log("Windows os detected. setting file separator as " + FileSeperator, Enums.LEVEL.TRACE);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				FileSeperator = "\\";
				Logger.Log("Linux os detected. setting file separator as " + FileSeperator, Enums.LEVEL.TRACE);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				FileSeperator = "//";
				Logger.Log("OSX os detected. setting file separator as " + FileSeperator, Enums.LEVEL.TRACE);
			}
		}

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

		public static float GenerateUniqueIdentifier(Random prng) {
			int sign = prng.Next(2);
			int exponent = prng.Next((1 << 8) - 1);
			int mantissa = prng.Next(1 << 23);
			int bits = (sign << 31) + (exponent << 23) + mantissa;
			return IntBitsToFloat(bits);
		}

		private static float IntBitsToFloat(int bits) {
			unsafe {
				return *(float*) &bits;
			}
		}
		
		public static Timer? ScheduleTask(Action action, TimeSpan delay) {
			if (action == null) {
				Logger.Log("Action is null! " + nameof(action), Enums.LEVEL.ERROR);
				return null;
			}

			Timer? TaskSchedulerTimer = null;

			TaskSchedulerTimer = new Timer(e => {
				InBackgroundThread(action, "Task Scheduler");

				if (TaskSchedulerTimer != null) {
					TaskSchedulerTimer.Dispose();
					TaskSchedulerTimer = null;
				}
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
		
		public static string? ExecuteBash(this string cmd, bool sudo) {
			if(GetOsPlatform() != OSPlatform.Linux) {
				Logger.Log("Current OS environment isn't Linux.", Enums.LEVEL.ERROR);
				return null;
			}

			if (string.IsNullOrEmpty(cmd)) {
				return null;
			}

			string escapedArgs = cmd.Replace("\"", "\\\"");
			string args = $"-c \"{escapedArgs}\"";
			string argsWithSudo = $"-c \"sudo {escapedArgs}\"";

			using Process process = new Process() {
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

			return result;
		}
		
		public static string? GetLocalIpAddress() {
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
				if (endPoint != null) {
					return endPoint.Address.ToString();
				}
			}

			return null;
		}

		public static ConsoleKeyInfo? FetchUserInputSingleChar(TimeSpan delay) {
			Task<ConsoleKeyInfo> task = Task.Factory.StartNew(Console.ReadKey);
			ConsoleKeyInfo? result = Task.WaitAny(new Task[] { task }, delay) == 0 ? task.Result : (ConsoleKeyInfo?) null;
			return result;
		}

		public static void SetConsoleTitle(string text) => Console.Title = text;

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();

		public static string? GetExternalIp() {
			if (!IsNetworkAvailable()) {
				return null;
			}

			try {
				using WebClient client = new WebClient();
				string result = client.DownloadString("https://api.ipify.org/").Trim('\n');
				return result;
			}
			catch {
				return null;
			}
		}

		public static void GenerateAsciiFromText(string text) {
			if (string.IsNullOrEmpty(text)) {
				Logger.Log("The specified text is empty or null", Enums.LEVEL.WARN);
				return;
			}

			Logger.Log(FiggleFonts.Ogre.Render(text), Enums.LEVEL.GREEN);
		}

		public static string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine) => Environment.GetEnvironmentVariable(variable, target);

		public static bool SetEnvironmentVariable(string variableName, string variableValue, EnvironmentVariableTarget target) {
			try {
				Environment.SetEnvironmentVariable(variableName, variableValue, target);
				return true;
			}
			catch (Exception e) {
				Logger.Log(e);
				return false;
			}
		}

		public static DateTime ConvertTo24Hours(DateTime source) =>
			DateTime.TryParse(source.ToString("yyyy MMMM d HH:mm:ss tt"), out DateTime result) ? result : DateTime.Now;

		public static DateTime ConvertTo12Hours(DateTime source) =>
			DateTime.TryParse(source.ToString("dddd, dd MMMM yyyy"), out DateTime result) ? result : DateTime.Now;

		public static string GetLocalIPv4(NetworkInterfaceType typeOfNetwork) {
			string output = "";
			foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces()) {
				if (item.NetworkInterfaceType == typeOfNetwork && item.OperationalStatus == OperationalStatus.Up) {
					foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses) {
						if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
							output = ip.Address.ToString();
						}
					}
				}
			}
			return output;
		}

		public static string? GetUrlToString(string url, Method method) {
			if (!IsNetworkAvailable()) {
				Logger.Log("Network is unavailable.", Enums.LEVEL.WARN);
				return null;
			}

			if (string.IsNullOrEmpty(url)) {
				return null;
			}

			RestClient client = new RestClient(url);
			RestRequest request = new RestRequest(method);
			request.AddHeader("cache-control", "no-cache");
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
				return null;
			}

			return response.Content;
		}

		public static string? GetUrlToString(this string url) {
			if (!IsNetworkAvailable()) {
				Logger.Log("Network is unavailable.", Enums.LEVEL.WARN);
				return null;
			}

			if (string.IsNullOrEmpty(url)) {
				return null;
			}

			IRestResponse response = new RestClient(url).Execute(new RestRequest(Method.GET));

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
				return null;
			}

			return response.Content;
		}

		public static byte[]? GetUrlToBytes(string url, Method method, string userAgent, string? headerName = null, string? headerValue = null) {
			if (!IsNetworkAvailable()) {
				Logger.Log("Cannot process, network is unavailable.", Enums.LEVEL.WARN);
				return new byte[0];
			}

			if (url == null) {
				throw new ArgumentNullException(nameof(url));
			}

			RestClient client = new RestClient(url);
			RestRequest request = new RestRequest(method);
			client.UserAgent = userAgent;
			request.AddHeader("cache-control", "no-cache");

			if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrEmpty(headerValue)) {
				request.AddHeader(headerName, headerValue);
			}

			Logger.Log("Downloading bytes...", Enums.LEVEL.TRACE);
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
				return null;
			}

			Logger.Log("Successfully downloaded", Enums.LEVEL.TRACE);
			return response.RawBytes;
		}

		public static string GetFileName(string? path) {
			if (string.IsNullOrEmpty(path)) {
				return string.Empty;
			}

			if (GetOsPlatform().Equals(OSPlatform.Windows)) {
				return Path.GetFileName(path) ?? string.Empty;
			}

			return path.Substring(path.LastIndexOf(FileSeperator, StringComparison.Ordinal) + 1);
		}

		public static string ReadLineMasked(char mask = '*') {
			StringBuilder result = new StringBuilder();

			ConsoleKeyInfo keyInfo;
			while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter) {
				if (!char.IsControl(keyInfo.KeyChar)) {
					result.Append(keyInfo.KeyChar);
					Console.Write(mask);
				}
				else if ((keyInfo.Key == ConsoleKey.Backspace) && (result.Length > 0)) {
					result.Remove(result.Length - 1, 1);

					if (Console.CursorLeft == 0) {
						Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
						Console.Write(' ');
						Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
					}
					else {

						// There are two \b characters here
						Console.Write(@" ");
					}
				}
			}

			Console.WriteLine();
			return result.ToString();
		}

		public static void WriteBytesToFile(byte[] bytesToWrite, string filePath) {
			if (bytesToWrite.Length <= 0 || string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath)) {
				return;
			}

			File.WriteAllBytes(filePath, bytesToWrite);
		}

		public static Thread? InBackgroundThread(Action action, string threadName, bool longRunning = false) {
			if (action == null) {
				Logger.Log("Action is null! " + nameof(action), Enums.LEVEL.ERROR);
				return null;
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning) {
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = threadName;
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
			return BackgroundThread;
		}

		public static Thread? InBackgroundThread(Action action, bool longRunning = false) {
			if (action == null) {
				Logger.Log("Action is null! " + nameof(action), Enums.LEVEL.ERROR);
				return null;
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning) {
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = action.GetHashCode().ToString();
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
			return BackgroundThread;
		}

		public static void InBackground(Action action, bool longRunning = false) {
			if (action == null) {
				Logger.Log("Action is null! " + nameof(action), Enums.LEVEL.ERROR);
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static void ExecuteCommand(string command, bool redirectOutput = false, string fileName = "/bin/bash") {
			if (GetOsPlatform() != OSPlatform.Linux && fileName == "/bin/bash") {
				Logger.Log($"Current OS environment isn't Linux.", Enums.LEVEL.ERROR);
				return;
			}

			try {
				Process proc = new Process {
					StartInfo = {
						FileName = fileName,
						Arguments = "-c \" " + command + " \"",
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}
				};


				proc.StartInfo.RedirectStandardOutput = redirectOutput;

				proc.Start();
				proc.WaitForExit(4000);

				if (redirectOutput) {
					while (!proc.StandardOutput.EndOfStream) {
						string? output = proc.StandardOutput.ReadLine();
						if (output != null) {
							Logger.Log(output, Enums.LEVEL.TRACE);
						}
					}
				}
			}
			catch (PlatformNotSupportedException) {
				Logger.Log("Platform not supported exception thrown, internal error, cannot proceed.", Enums.LEVEL.WARN);
			}
			catch (Win32Exception) {
				Logger.Log("System cannot find the specified file.", Enums.LEVEL.ERROR);
			}
			catch (ObjectDisposedException) {
				Logger.Log("Object has been disposed already.", Enums.LEVEL.ERROR);
			}
			catch (InvalidOperationException) {
				Logger.Log("Invalid operation exception, internal error.", Enums.LEVEL.ERROR);
			}
		}

		public static void InBackground<T>(Func<T> function, bool longRunning = false) {
			if (function == null) {
				Logger.Log("Function is null! " + nameof(function), Enums.LEVEL.ERROR);
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(function, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static async Task<IList<T>?> InParallel<T>(IEnumerable<Task<T>> tasks) {
			if (tasks == null) {
				Logger.Log(nameof(tasks), Enums.LEVEL.WARN);
				return null;
			}

			IList<T> results = await Task.WhenAll(tasks).ConfigureAwait(false);
			return results;
		}

		public static async Task InParallel(IEnumerable<Task> tasks) {
			if (tasks == null) {
				Logger.Log(nameof(tasks), Enums.LEVEL.WARN);
				return;
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		public static bool IsNetworkAvailable() {
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
				Logger.Log(e);
				return false;
			}
		}

		public static bool IsNetworkAvailable(bool usingWebClient) {
			try {
				using WebClient client = new WebClient();
				using Stream stream = client.OpenRead("http://www.google.com");
				return true;
			}
			catch (Exception) {
				return false;
			}
		}

		public static void CloseProcess(string processName) {
			if (string.IsNullOrEmpty(processName) || string.IsNullOrWhiteSpace(processName)) {
				return;
			}

			Process[] workers = Process.GetProcessesByName(processName);
			foreach (Process worker in workers) {
				worker.Kill();
				Logger.Log($"Closed {processName} process.");
				worker.WaitForExit();
				worker.Dispose();
			}
		}

		public static void CheckMultipleProcess() {
			string RunningProcess = Process.GetCurrentProcess().ProcessName;
			Process[] processes = Process.GetProcessesByName(RunningProcess);

			if (processes.Length > 1) {
				while (true) {
					Logger.Log("There are multiple instance of current program running.", Enums.LEVEL.WARN);
					Logger.Log("> Press Y to close them and continue executing current process.");
					Logger.Log("> Press N to close current process and continue with the others.");

					char input = Console.ReadKey().KeyChar;

					switch (input) {
						case 'y':
							int procCounter = 0;
							foreach (Process proc in processes) {
								if (proc.Id != Process.GetCurrentProcess().Id) {
									proc.Kill();
									procCounter++;
									Logger.Log($"Killed {procCounter} processes.", Enums.LEVEL.WARN);
								}
							}
							return;

						case 'n':
							Logger.Log("Exiting current process as another instance of same application is running...");
							Process.GetCurrentProcess().Kill();
							return;

						default:
							Logger.Log("Unknown key pressed... try again!", Enums.LEVEL.WARN);
							continue;
					}
				}
			}
		}
	}
}
