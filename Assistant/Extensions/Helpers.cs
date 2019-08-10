
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.AssistantCore;
using Assistant.Log;
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
using Unosquare.RaspberryIO;
using static Assistant.AssistantCore.Enums;
using ProcessThread = System.Diagnostics.ProcessThread;
using TaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace Assistant.Extensions {

	public static class Helpers {
		private static readonly Logger Logger = new Logger("HELPERS");

		private static string FileSeperator { get; set; } = @"\";

		public static void InBackgroundThread(Action action) {
			if (action == null) {
				Logger.Log("Action is null.", Enums.LogLevels.Warn);
				return;
			}

			if (Core.DisablePiMethods) {
				float identifer = GenerateTaskIdentifier(new Random());
				InBackgroundThread(action, identifer.ToString());
			}
			else {
				Pi.Threading.StartThread(action);
			}
		}

		public static void SetFileSeperator() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				FileSeperator = "//";
				Logger.Log("Windows os detected. setting file separator as " + FileSeperator, Enums.LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				FileSeperator = "\\";
				Logger.Log("Linux os detected. setting file separator as " + FileSeperator, Enums.LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				FileSeperator = "//";
				Logger.Log("OSX os detected. setting file separator as " + FileSeperator, Enums.LogLevels.Trace);
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

		public static float GenerateTaskIdentifier(Random prng) {
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

		public static ProcessThread FetchThreadById(int id) {
			ProcessThreadCollection currentThreads = Process.GetCurrentProcess().Threads;

			foreach (ProcessThread thread in currentThreads) {
				if (thread.Id.Equals(id)) {
					return thread;
				}
			}

			return null;
		}

		public static void ScheduleTask(TaskStructure structure, TimeSpan delay, bool longrunning) {
			if (structure == null) {
				Logger.Log("Action is null! " + nameof(structure), Enums.LogLevels.Error);
				return;
			}

			Timer TaskSchedulerTimer = null;

			TaskSchedulerTimer = new Timer(e => {
				InBackground(() => structure.Task, longrunning);

				if (TaskSchedulerTimer != null) {
					TaskSchedulerTimer.Dispose();
					TaskSchedulerTimer = null;
				}
			}, null, delay, delay);
		}

		public static void ScheduleTask(Action action, TimeSpan delay) {
			if (action == null) {
				Logger.Log("Action is null! " + nameof(action), Enums.LogLevels.Error);
				return;
			}

			Timer TaskSchedulerTimer = null;

			TaskSchedulerTimer = new Timer(e => {
				InBackgroundThread(action, "Task Scheduler");

				if (TaskSchedulerTimer != null) {
					TaskSchedulerTimer.Dispose();
					TaskSchedulerTimer = null;
				}
			}, null, delay, delay);
		}

		public static bool IsSocketConnected(Socket s) {
			bool part1 = s.Poll(1000, SelectMode.SelectRead);
			bool part2 = s.Available == 0;
			if (part1 && part2) {
				return false;
			}

			return true;
		}

		public static void PlayNotification(Enums.NotificationContext context = Enums.NotificationContext.Normal, bool redirectOutput = false) {
			if (Core.IsUnknownOs) {
				Logger.Log("Cannot proceed as the running operating system is unknown.", Enums.LogLevels.Error);
				return;
			}

			if (Core.Config.MuteAssistant) {
				Logger.Log("Notifications are muted in config.", Enums.LogLevels.Trace);
				return;
			}

			if (!Directory.Exists(Constants.ResourcesDirectory)) {
				Logger.Log("Resources directory doesn't exist!", Enums.LogLevels.Warn);
				return;
			}

			switch (context) {
				case Enums.NotificationContext.Imap:
					if (!File.Exists(Constants.IMAPPushNotificationFilePath)) {
						Logger.Log("IMAP notification music file doesn't exist!", Enums.LogLevels.Warn);
						return;
					}

					ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.IMAPPushFileName} -q", Core.Config.Debug || redirectOutput);
					Logger.Log("Notification command processed sucessfully!", Enums.LogLevels.Trace);
					break;

				case Enums.NotificationContext.EmailSend:
					break;

				case Enums.NotificationContext.EmailSendFailed:
					break;

				case Enums.NotificationContext.FatalError:
					break;

				case Enums.NotificationContext.Normal:
					if (!Core.IsNetworkAvailable) {
						Logger.Log("Cannot process, network is unavailable.", Enums.LogLevels.Warn);
					}
					break;
			}
		}

		public static string GetLocalIpAddress() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) {
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}

		public static ConsoleKeyInfo? FetchUserInputSingleChar(TimeSpan delay) {
			Task<ConsoleKeyInfo> task = Task.Factory.StartNew(Console.ReadKey);
			ConsoleKeyInfo? result = Task.WaitAny(new Task[] { task }, delay) == 0 ? task.Result : (ConsoleKeyInfo?) null;
			return result;
		}

		public static void SetConsoleTitle(string text) => Console.Title = $"{Core.AssistantName} V{Constants.Version} | {text}";

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}

		public static string GetExternalIp() {
			if (Core.IsNetworkAvailable) {
				string result = new WebClient().DownloadString("https://api.ipify.org/").Trim('\n');
				return result;
			}

			Logger.Log("No internet connection.", Enums.LogLevels.Error);
			return null;
		}

		internal static string TimeRan() {
			DateTime StartTime = Core.StartupTime;
			TimeSpan dt = DateTime.Now - StartTime;
			string Duration = "Online for: ";
			Duration += Math.Round(dt.TotalDays, 0) + " days, ";
			Duration += Math.Round(dt.TotalHours, 0) + " hours, ";
			Duration += Math.Round(dt.TotalMinutes, 0) + " minutes.";
			return Duration;
		}

		public static void GenerateAsciiFromText(string text) {
			if (IsNullOrEmpty(text)) {
				Logger.Log("The specified text is empty or null", Enums.LogLevels.Warn);
				return;
			}

			Logger.Log(FiggleFonts.Ogre.Render(text), Enums.LogLevels.Ascii);
		}

		public static string FetchVariable(int arrayLine, bool returnParsed = false, string varName = null) {
			if (!File.Exists(Constants.VariablesPath)) {
				Logger.Log("Variables file doesnt exist! aborting...", Enums.LogLevels.Error);
				return null;
			}

			string[] variables = File.ReadAllLines(Constants.VariablesPath);

			if (!string.IsNullOrEmpty(variables[arrayLine]) || !string.IsNullOrWhiteSpace(variables[arrayLine])) {
				if (returnParsed) {
					return ParseVariable(variables[arrayLine], varName);
				}

				return variables[arrayLine];
			}

			Logger.Log("Line is empty.", Enums.LogLevels.Error);
			return null;
		}

		private static string ParseVariable(string variableRaw, string variableName, char seperator = '=') {
			if (string.IsNullOrEmpty(variableRaw) || string.IsNullOrWhiteSpace(variableRaw)) {
				Logger.Log("Variable is empty.", Enums.LogLevels.Error);
				return null;
			}

			if (string.IsNullOrEmpty(variableName) || string.IsNullOrWhiteSpace(variableName)) {
				Logger.Log("Variable name is empty.", Enums.LogLevels.Error);
				return null;
			}

			string[] raw = variableRaw.Split(seperator);

			if (raw[0].Equals(variableName, StringComparison.OrdinalIgnoreCase)) {
				return raw[1];
			}

			Logger.Log("Failed to parse variable.", Enums.LogLevels.Error);
			return null;
		}

		public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine) => Environment.GetEnvironmentVariable(variable, target);

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

		public static string GetUrlToString(string url, string Method = "GET", bool withuserAgent = true) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", Enums.LogLevels.Warn);
				return null;
			}

			string Response = null;

			try {
				Uri address = new Uri(url);
				HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
				request.Method = Method;

				if (withuserAgent) {
					request.UserAgent = Constants.GitHubUserID;
				}

				request.ContentType = "text/xml";
				using (HttpWebResponse response = request.GetResponse() as HttpWebResponse) {
					StreamReader reader = new StreamReader(response.GetResponseStream());
					Response = reader.ReadToEnd();
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return null;
			}

			return Response;
		}

		public static DateTime ConvertTo24Hours
			(DateTime source) =>
			DateTime.TryParse(source.ToString("yyyy MMMM d HH:mm:ss tt"), out DateTime result) ? result : DateTime.Now;

		public static DateTime ConvertTo12Hours
			(DateTime source) =>
			DateTime.TryParse(source.ToString("dddd, dd MMMM yyyy"), out DateTime result) ? result : DateTime.Now;

		public static string GetUrlToString(string url, Method method, bool withuseragent = true) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", Enums.LogLevels.Warn);
				return null;
			}

			if (url == null) {
				throw new ArgumentNullException(nameof(url));
			}

			try {
				RestClient client = new RestClient(url);
				RestRequest request = new RestRequest(method);

				if (withuseragent) {
					client.UserAgent = Constants.GitHubProjectName;
				}

				request.AddHeader("cache-control", "no-cache");
				IRestResponse response = client.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK) {
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
					return null;
				}

				return response.Content;
			}
			catch (PlatformNotSupportedException) {
				Logger.Log("Platform not supported exception during rest request.", Enums.LogLevels.Trace);
				return null;
			}
		}

		public static byte[] GetUrlToBytes(string url, Method method, string userAgent, string headerName = null, string headerValue = null) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", Enums.LogLevels.Warn);
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

			Logger.Log("Downloading bytes...", Enums.LogLevels.Trace);
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
				return null;
			}

			Logger.Log("Successfully downloaded", Enums.LogLevels.Trace);
			return response.RawBytes;
		}

		public static async Task<bool> RestartOrExit(bool Restart = false) {
			if (Restart) {
				Logger.Log("Restarting...");
				await Task.Delay(5000).ConfigureAwait(false);
				await Core.Restart().ConfigureAwait(false);
				return true;
			}

			Logger.Log("Exiting...");
			await Task.Delay(5000).ConfigureAwait(false);
			await Core.Exit().ConfigureAwait(false);
			return true;
		}

		public static string GetFileName(string path) {
			if (GetOsPlatform().Equals(OSPlatform.Windows)) {
				return Path.GetFileName(path);
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

		public static (int, Thread) InBackgroundThread(Action action, string threadName, bool longRunning = false) {
			if (action == null) {
				Logger.Log("Action is null! " + nameof(action), Enums.LogLevels.Error);
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
				Logger.Log("Action is null! " + nameof(action), Enums.LogLevels.Error);
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static void ExecuteCommand(string command, bool redirectOutput = false, string fileName = "/bin/bash") {
			if (Core.IsUnknownOs && fileName == "/bin/bash") {
				Logger.Log($"{Core.AssistantName} is running on unknown OS. command cannot be executed.", Enums.LogLevels.Error);
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
						Logger.Log(proc.StandardOutput.ReadLine(), Enums.LogLevels.Trace);
					}
				}
			}
			catch (PlatformNotSupportedException) {
				Logger.Log("Platform not supported exception thrown, internal error, cannot proceed.", Enums.LogLevels.Warn);
			}
			catch (Win32Exception) {
				Logger.Log("System cannot find the specified file.", Enums.LogLevels.Error);
			}
			catch (ObjectDisposedException) {
				Logger.Log("Object has been disposed already.", Enums.LogLevels.Error);
			}
			catch (InvalidOperationException) {
				Logger.Log("Invalid operation exception, internal error.", Enums.LogLevels.Error);
			}
		}

		public static void InBackground<T>(Func<T> function, bool longRunning = false) {
			if (function == null) {
				Logger.Log("Function is null! " + nameof(function), Enums.LogLevels.Error);
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
				Logger.Log(nameof(tasks), Enums.LogLevels.Warn);
				return null;
			}

			IList<T> results = await Task.WhenAll(tasks).ConfigureAwait(false);
			return results;
		}

		public static bool IsRaspberryEnvironment() {
			try {
				if (GetOsPlatform() == OSPlatform.Linux
					&& Pi.Info.RaspberryPiVersion.ToString().Equals("Pi3ModelBEmbest", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}
			catch {
				return false;
			}

			return false;
		}

		public static async Task InParallel(IEnumerable<Task> tasks) {
			if (tasks == null) {
				Logger.Log(nameof(tasks), Enums.LogLevels.Warn);
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
				Logger.Log(e);
				return false;
			}
		}

		public static bool CheckForInternetConnection(bool usingWebClient) {
			try {
				using (WebClient client = new WebClient())
				using (Stream stream = client.OpenRead("http://www.google.com")) {
					return true;
				}
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

		public static bool IsNullOrEmpty(string value) => string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);

		public static void CheckMultipleProcess() {
			string RunningProcess = Process.GetCurrentProcess().ProcessName;
			Process[] processes = Process.GetProcessesByName(RunningProcess);

			if (processes.Length > 1) {
				while (true) {
					Logger.Log("There are multiple instance of current program running.", LogLevels.Warn);
					Logger.Log("> Press Y to close them and continue executing current process.");
					Logger.Log("> Press N to close current process and continue with the others.");

					char input = Console.ReadKey().KeyChar;

					switch (input) {
						case 'y': {
								int procCounter = 0;
								foreach (Process proc in processes) {
									if (proc.Id != Process.GetCurrentProcess().Id) {
										proc.Kill();
										procCounter++;
										Logger.Log($"Killed {procCounter} processes.", LogLevels.Warn);
									}
								}
								return;
							}

						case 'n': {
								Logger.Log("Exiting current process as another instance of same application is running...");
								Process.GetCurrentProcess().Kill();
								return;
							}

						default: {
								Logger.Log("Unknown key pressed... try again!", LogLevels.Warn);
								continue;
							}
					}
				}


			}
		}
	}
}
