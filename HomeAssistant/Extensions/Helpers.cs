using HomeAssistant.Core;
using HomeAssistant.Log;
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
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Extensions {

	public static class Helpers {
		private static readonly Logger Logger = new Logger("HELPERS");
		private static Timer TaskSchedulerTimer;
		private static string FileSeprator = @"\";

		public static void SetFileSeperator() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				FileSeprator = @"/";
				Logger.Log("windows os decteted. setting file seperator as " + FileSeprator, LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				FileSeprator = @"\";
				Logger.Log("linux os decteted. setting file seperator as " + FileSeprator, LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				FileSeprator = @"/";
				Logger.Log("osx os decteted. setting file seperator as " + FileSeprator, LogLevels.Trace);
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

		public static bool IsSocketConnected(Socket s) {
			bool part1 = s.Poll(1000, SelectMode.SelectRead);
			bool part2 = s.Available == 0;
			if (part1 && part2) {
				return false;
			}
			else {
				return true;
			}
		}

		public static void PlayNotification(NotificationContext context = NotificationContext.Normal, bool redirectOutput = false) {
			if (Tess.IsUnknownOS) {
				Logger.Log("Cannot proceed as the running operating system is unknown.", LogLevels.Error);
				return;
			}

			if (Tess.Config.MuteAll) {
				Logger.Log("Notifications are muted in config.", LogLevels.Trace);
				return;
			}

			if (!Directory.Exists(Constants.ResourcesDirectory)) {
				Logger.Log("Resources directory doesn't exist!", LogLevels.Warn);
				return;
			}

			switch (context) {
				case NotificationContext.Imap:
					if (!File.Exists(Constants.IMAPPushNotificationFilePath)) {
						Logger.Log("IMAP notification music file doesn't exist!", LogLevels.Warn);
						return;
					}

					ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.IMAPPushFileName} -q", Tess.Config.Debug ? true : redirectOutput);
					Logger.Log("Notification command processed sucessfully!", LogLevels.Trace);
					break;

				case NotificationContext.EmailSend:
					break;

				case NotificationContext.EmailSendFailed:
					break;

				case NotificationContext.FatalError:
					break;

				case NotificationContext.Normal:
					if (!Tess.IsNetworkAvailable) {
						Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
						return;
					}
					break;

				default:
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

		public static ConsoleKeyInfo? FetchUserInputSingleChar(TimeSpan delay) {
			Task<ConsoleKeyInfo> task = Task.Factory.StartNew(Console.ReadKey);
			ConsoleKeyInfo? result = Task.WaitAny(new Task[] { task }, delay) == 0 ? task.Result : (ConsoleKeyInfo?) null;
			return result;
		}

		public static void SetConsoleTitle(string text) => Console.Title = $"TESS Home Assistant V{Constants.Version} | {text}";

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}

		public static string GetExternalIP() {
			if (Tess.IsNetworkAvailable) {
				string result = new WebClient().DownloadString("https://api.ipify.org/").Trim('\n');
				return result;
			}
			else {
				Logger.Log("No internet connection.", LogLevels.Error);
				return null;
			}
		}

		internal static string TimeRan() {
			DateTime StartTime = Tess.StartupTime;
			TimeSpan dt = DateTime.Now - StartTime;
			string Duration = "Online for: ";
			Duration += Math.Round(dt.TotalDays, 0).ToString() + " days, ";
			Duration += Math.Round(dt.TotalHours, 0).ToString() + " hours, ";
			Duration += Math.Round(dt.TotalMinutes, 0).ToString() + " minutes.";
			return Duration.ToString();
		}

		public static async Task DisplayTessASCII() {
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
				Logger.Log(e, LogLevels.Error);
				return false;
			}
		}

		public static string GetUrlToString(string url, string Method = "GET", bool withuserAgent = true) {
			if (!Tess.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
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
				Logger.Log(e, LogLevels.Error);
				return null;
			}

			return Response;
		}

		public static string GetUrlToString(string url, Method method, bool withuseragent = true) {
			if (!Tess.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
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
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus.ToString());
					return null;
				}

				return response.Content;
			}
			catch (PlatformNotSupportedException) {
				Logger.Log("Platform not supported exception during rest request.", LogLevels.Trace);
				return null;
			}
		}

		public static byte[] GetUrlToBytes(string url, Method method, string userAgent, string headerName = null, string headerValue = null) {
			if (!Tess.IsNetworkAvailable) {
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
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

			Logger.Log("Downloading bytes...", LogLevels.Trace);
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus.ToString());
				return null;
			}

			Logger.Log("Sucessfully downloaded", LogLevels.Trace);
			return response.RawBytes;
		}

		public static async Task<bool> RestartOrExit(bool Restart = false) {
			if (Restart) {
				Logger.Log("Restarting...");
				await Task.Delay(5000).ConfigureAwait(false);
				await Tess.Restart().ConfigureAwait(false);
				return true;
			}
			else {
				Logger.Log("Exiting...");
				await Task.Delay(5000).ConfigureAwait(false);
				await Tess.Exit().ConfigureAwait(false);
				return true;
			}
		}

		public static string GetFileName(string path) => path.Substring(path.LastIndexOf(FileSeprator) + 1);

		public static void WriteBytesToFile(byte[] bytesToWrite, string filePath) {
			if (bytesToWrite.Length <= 0 || string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath)) {
				return;
			}

			File.WriteAllBytes(filePath, bytesToWrite);
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
			if (Tess.IsUnknownOS && fileName == "/bin/bash") {
				Logger.Log("TESS is running on unknown OS. command cannot be executed.", LogLevels.Error);
				return;
			}

			try {
				Process proc = new Process();
				proc.StartInfo.FileName = fileName;
				proc.StartInfo.Arguments = "-c \" " + command + " \"";
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.CreateNoWindow = true;
				proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;


				if (redirectOutput) {
					proc.StartInfo.RedirectStandardOutput = true;
				}
				else {
					proc.StartInfo.RedirectStandardOutput = false;
				}

				proc.Start();
				proc.WaitForExit(4000);

				if (redirectOutput) {
					while (!proc.StandardOutput.EndOfStream) {
						Logger.Log(proc.StandardOutput.ReadLine(), LogLevels.Trace);
					}
				}
			}
			catch (PlatformNotSupportedException) {
				Logger.Log("Platform not supported exception thrown, internal error, cannot proceed.", LogLevels.Warn);
				return;
			}
			catch (Win32Exception) {
				Logger.Log("System cannot find the specified file.", LogLevels.Error);
				return;
			}
			catch (ObjectDisposedException) {
				Logger.Log("Object has been disposed already.", LogLevels.Error);
				return;
			}
			catch (InvalidOperationException) {
				Logger.Log("Invalid operation exception, internal error.", LogLevels.Error);
				return;
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
				Ping myPing = new Ping();
				string host = "8.8.8.8";
				byte[] buffer = new byte[32];
				int timeout = 1000;
				PingOptions pingOptions = new PingOptions();
				PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
				return reply.Status == IPStatus.Success;
			}
			catch (Exception) {
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
