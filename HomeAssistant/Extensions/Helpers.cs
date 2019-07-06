<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
=======
using HomeAssistant.AssistantCore;
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
using HomeAssistant.Log;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
using static HomeAssistant.Core.Enums;
=======
using static HomeAssistant.AssistantCore.Enums;
using ProcessThread = System.Diagnostics.ProcessThread;
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs

namespace HomeAssistant.Extensions {
	public static class Helpers {
		private static readonly Logger Logger = new Logger("HELPERS");
		private static Timer TaskSchedulerTimer;
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
=======
		private static string FileSeperator { get; set; } = @"\";

		public static void InBackgroundThread(Action action) => Pi.Threading.StartThread(action);

		public static void SetFileSeperator() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				FileSeperator = "//";
				Logger.Log("Windows os detected. setting file separator as " + FileSeperator, LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				FileSeperator = "\\";
				Logger.Log("Linux os detected. setting file separator as " + FileSeperator, LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				FileSeperator = "//";
				Logger.Log("OSX os detected. setting file separator as " + FileSeperator, LogLevels.Trace);
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
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs

		public static void ScheduleTask(Action action, TimeSpan delay) {
			if (action == null) {
				Logger.Log($"Action is null! " + nameof(action), LogLevels.Error);
				return;
			}

			if (delay == null) {
				Logger.Log($"Delay is null! " + nameof(action), LogLevels.Error);
				return;
			}
			
			if (TaskSchedulerTimer != null) {
				TaskSchedulerTimer.Dispose();
				TaskSchedulerTimer = null;
			}

			if (TaskSchedulerTimer == null) {
				TaskSchedulerTimer = new Timer(e => {
					InBackgroundThread(action, "Task Scheduler");

					if (TaskSchedulerTimer != null) {
						TaskSchedulerTimer.Dispose();
						TaskSchedulerTimer = null;
					}

				}, null, delay, delay);
			}
		}

<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
		//TODO Implement play notification sound
		public static void PlayNotification(NotificationContext context = NotificationContext.Normal) {
=======
		public static void PlayNotification(NotificationContext context = NotificationContext.Normal, bool redirectOutput = false) {
			if (Core.IsUnknownOs) {
				Logger.Log("Cannot proceed as the running operating system is unknown.", LogLevels.Error);
				return;
			}

			if (Core.Config.MuteAssistant) {
				Logger.Log("Notifications are muted in config.", LogLevels.Trace);
				return;
			}

>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
			if (!Directory.Exists(Constants.ResourcesDirectory)) {
				Logger.Log("Resources directory doesn't exist!", LogLevels.Warn);
				return;
			}

			switch (context) {
				case NotificationContext.Imap:
					if (!File.Exists(Constants.ResourcesDirectory + "/" + Constants.IMAPSoundName)) {
						Logger.Log("IMAP notification music file doesn't exist!", LogLevels.Warn);
						return;
					}

<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
					ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/{Constants.ResourcesDirectory} && omxplayer {Constants.IMAPSoundName}", true);
=======
					ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.IMAPPushFileName} -q", AssistantCore.Core.Config.Debug || redirectOutput);
					Logger.Log("Notification command processed sucessfully!", LogLevels.Trace);
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
					break;
				case NotificationContext.EmailSend:
					break;
				case NotificationContext.EmailSendFailed:
					break;
				case NotificationContext.FatalError:
					break;
				case NotificationContext.Normal:
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
					break;
				default:
=======
					if (!Core.IsNetworkAvailable) {
						Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
					}
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
					break;
			}
		}

		public static string GetLocalIPAddress() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) {
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}

<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
		public static void SetConsoleTitle(string text) => Console.Title = $"TESS Home Assistant V{Constants.Version} | {text}";
=======
		public static ConsoleKeyInfo? FetchUserInputSingleChar(TimeSpan delay) {
			Task<ConsoleKeyInfo> task = Task.Factory.StartNew(Console.ReadKey);
			ConsoleKeyInfo? result = Task.WaitAny(new Task[] { task }, delay) == 0 ? task.Result : (ConsoleKeyInfo?) null;
			return result;
		}

		public static void SetConsoleTitle(string text) => Console.Title = $"{Core.AssistantName} Home Assistant V{Constants.Version} | {text}";
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}

<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
		public static string GetExternalIP() => new WebClient().DownloadString("https://api.ipify.org/").Trim('\n');

		internal static string TimeRan() {
			DateTime StartTime = Program.StartupTime;
=======
		public static string GetExternalIp() {
			if (Core.IsNetworkAvailable) {
				string result = new WebClient().DownloadString("https://api.ipify.org/").Trim('\n');
				return result;
			}

			Logger.Log("No internet connection.", LogLevels.Error);
			return null;
		}

		internal static string TimeRan() {
			DateTime StartTime = Core.StartupTime;
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
			TimeSpan dt = DateTime.Now - StartTime;
			string Duration = "Online for: ";
			Duration += Math.Round(dt.TotalDays, 0).ToString() + " days, ";
			Duration += Math.Round(dt.TotalHours, 0).ToString() + " hours, ";
			Duration += Math.Round(dt.TotalMinutes, 0).ToString() + " minutes.";
			return Duration.ToString();
		}

		public static async Task DisplayAssistantASCII() {
			Logger.Log(@"  _______ ______  _____ _____ ", LogLevels.Ascii);
			await Task.Delay(300).ConfigureAwait(false);
			Logger.Log(@" |__   __|  ____|/ ____/ ____|", LogLevels.Ascii);
			await Task.Delay(300).ConfigureAwait(false);
			Logger.Log(@"    | |  | |__  | (___| (___  ", LogLevels.Ascii);
			await Task.Delay(300).ConfigureAwait(false);
			Logger.Log(@"    | |  |  __|  \___ \\___ \ ", LogLevels.Ascii);
			await Task.Delay(300).ConfigureAwait(false);
			Logger.Log(@"    | |  | |____ ____) |___) |", LogLevels.Ascii);
			await Task.Delay(300).ConfigureAwait(false);
			Logger.Log(@"    |_|  |______|_____/_____/ ", LogLevels.Ascii);
			await Task.Delay(100).ConfigureAwait(false);
			Logger.Log("\n", LogLevels.Ascii);
		}

		public static string FetchVariable(int arrayLine, bool returnParsed = false, string varName = null) {
			if (!File.Exists(Constants.VariablesPath)) {
				Logger.Log("Variables file doesnt exist! aborting...", LogLevels.Error);
				return null;
			}

			string[] variables = File.ReadAllLines(Constants.VariablesPath);

			if (!string.IsNullOrEmpty(variables[arrayLine]) || !string.IsNullOrWhiteSpace(variables[arrayLine])) {
				if (returnParsed) {
					return ParseVariable(variables[arrayLine], varName, '=');
				}
				else {
					return variables[arrayLine];
				}
			}
			else {
				Logger.Log("Line is empty.", LogLevels.Error);
				return null;
			}
		}

		private static string ParseVariable(string variableRaw, string variableName, char seperator = '=') {
			if (string.IsNullOrEmpty(variableRaw) || string.IsNullOrWhiteSpace(variableRaw)) {
				Logger.Log("Variable is empty.", LogLevels.Error);
				return null;
			}

			if (string.IsNullOrEmpty(variableName) || string.IsNullOrWhiteSpace(variableName)) {
				Logger.Log("Variable name is empty.", LogLevels.Error);
				return null;
			}

			string[] raw = variableRaw.Split(seperator);

			if (raw[0].Equals(variableName, StringComparison.OrdinalIgnoreCase)) {
				return raw[1];
			}
			else {
				Logger.Log("Failed to parse variable.", LogLevels.Error);
				return null;
			}
		}

		public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine) => Environment.GetEnvironmentVariable(variable, target);

		public static bool SetEnvironmentVariable(string variableName, string variableValue, EnvironmentVariableTarget target) {
			try {
				Environment.SetEnvironmentVariable(variableName, variableValue, target);
				return true;
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Error);
				return false;
			}
		}

		public static string GetUrlToString(string url, string Method = "GET", bool withuserAgent = true) {
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
=======
			if (!AssistantCore.Core.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
				return null;
			}

>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
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
				Logger.Log(e, ExceptionLogLevels.Error);
				return null;
			}

			return Response;
		}

		public static string GetUrlToString(string url, Method method, bool withuseragent = true) {
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
=======
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
				return null;
			}

>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
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
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus.ToString());
					return null;
				}

				return response.Content;
			}
			catch (Exception e) {
				if (e is PlatformNotSupportedException) {
					//Ignoring this as this is common on Raspberry pi with arm cpu system.
				}
			}
			return null;

<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
		}
=======
		public static byte[] GetUrlToBytes(string url, Method method, string userAgent, string headerName = null, string headerValue = null) {
			if (!AssistantCore.Core.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
				return new byte[0];
			}
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs

		public static byte[] GetUrlToBytes(string url, Method method, string headerName, string headerValue, string userAgent) {
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

			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus.ToString());
				return null;
			}

			return response.RawBytes;
		}

		public static async Task<bool> RestartOrExit(bool Restart = false) {
			if (Restart) {
				Logger.Log("Restarting...");
				await Task.Delay(5000).ConfigureAwait(false);
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
				await Program.Restart().ConfigureAwait(false);
				return true;
			}
			else {
				Logger.Log("Exiting...");
				await Task.Delay(5000).ConfigureAwait(false);
				await Program.Exit().ConfigureAwait(false);
				return true;
=======
				await AssistantCore.Core.Restart().ConfigureAwait(false);
				return true;
			}

			Logger.Log("Exiting...");
			await Task.Delay(5000).ConfigureAwait(false);
			await AssistantCore.Core.Exit().ConfigureAwait(false);
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
>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs
			}
		}

		public static void InBackgroundThread(Action action, string threadName, bool longRunning = false) {
			if (action == null) {
				Logger.Log($"Action is null! " + nameof(action), LogLevels.Error);
				return;
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning) {
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = threadName;
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
		}

		public static void InBackground(Action action, bool longRunning = false) {
			if (action == null) {
				Logger.Log($"Action is null! " + nameof(action), LogLevels.Error);
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static void ExecuteCommand(string command, bool redirectOutput = false, string fileName = "/bin/bash") {
<<<<<<< Updated upstream:HomeAssistant/Extensions/Helpers.cs
			Process proc = new Process();
			proc.StartInfo.FileName = fileName;
			proc.StartInfo.Arguments = "-c \" " + command + " \"";
=======
			if (Core.IsUnknownOs && fileName == "/bin/bash") {
				Logger.Log($"{Core.AssistantName} is running on unknown OS. command cannot be executed.", LogLevels.Error);
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

>>>>>>> Stashed changes:Assistant/Extensions/Helpers.cs

			if (redirectOutput) {
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
			}

			proc.Start();

			if (redirectOutput) {
				while (!proc.StandardOutput.EndOfStream) {
					Logger.Log(proc.StandardOutput.ReadLine(), LogLevels.Warn);
				}
			}
		}

		public static void InBackground<T>(Func<T> function, bool longRunning = false) {
			if (function == null) {
				Logger.Log($"Function is null! " + nameof(function), LogLevels.Error);
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
				Logger.Log(nameof(tasks), LogLevels.Warn);
				return null;
			}

			IList<T> results = await Task.WhenAll(tasks).ConfigureAwait(false);
			return results;
		}

		public static bool IsRaspberryEnvironment() {
			if (Pi.Info.RaspberryPiVersion.ToString().Equals("Pi3ModelBEmbest", StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
			else {
				return false;
			}
		}

		public static async Task InParallel(IEnumerable<Task> tasks) {
			if (tasks == null) {
				Logger.Log(nameof(tasks), LogLevels.Warn);
				return;
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		public static bool CheckForInternetConnection() {
			try {
				using (WebClient client = new WebClient())
				using (Stream stream = client.OpenRead("http://www.google.com")) {
					return true;
				}
			}
			catch {
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
				Logger.Log($"Exiting current process as another instance of same application is running...");
				Process.GetCurrentProcess().Kill();
			}
		}
	}
}
