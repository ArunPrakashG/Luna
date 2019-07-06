using HomeAssistant.AssistantCore;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using static HomeAssistant.AssistantCore.Enums;
using ProcessThread = System.Diagnostics.ProcessThread;

namespace HomeAssistant.Extensions
{

	public static class Helpers
	{
		private static readonly Logger Logger = new Logger("HELPERS");
		private static Timer TaskSchedulerTimer;
		private static string FileSeperator { get; set; } = @"\";

		public static void InBackgroundThread(Action action) => Pi.Threading.StartThread(action);

		public static void SetFileSeperator()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				FileSeperator = "//";
				Logger.Log("Windows os detected. setting file separator as " + FileSeperator, LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				FileSeperator = "\\";
				Logger.Log("Linux os detected. setting file separator as " + FileSeperator, LogLevels.Trace);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				FileSeperator = "//";
				Logger.Log("OSX os detected. setting file separator as " + FileSeperator, LogLevels.Trace);
			}
		}

		public static OSPlatform GetOsPlatform()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return OSPlatform.Windows;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return OSPlatform.Linux;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return OSPlatform.OSX;
			}

			return OSPlatform.Linux;
		}

		public static ProcessThread FetchThreadById(int id)
		{
			ProcessThreadCollection currentThreads = Process.GetCurrentProcess().Threads;

			foreach (ProcessThread thread in currentThreads)
			{
				if (thread.Id.Equals(id))
				{
					return thread;
				}
			}

			return null;
		}

		public static void ScheduleTask<T>(Func<T> action, TimeSpan delay, bool longrunning)
		{
			if (action == null)
			{
				Logger.Log("Action is null! " + nameof(action), LogLevels.Error);
				return;
			}

			if (TaskSchedulerTimer != null)
			{
				TaskSchedulerTimer.Dispose();
				TaskSchedulerTimer = null;
			}

			if (TaskSchedulerTimer == null)
			{
				TaskSchedulerTimer = new Timer(e => {
					InBackground(action, longrunning);

					if (TaskSchedulerTimer != null)
					{
						TaskSchedulerTimer.Dispose();
						TaskSchedulerTimer = null;
					}
				}, null, delay, delay);
			}
		}

		public static void ScheduleTask(Action action, TimeSpan delay)
		{
			if (action == null)
			{
				Logger.Log("Action is null! " + nameof(action), LogLevels.Error);
				return;
			}

			if (TaskSchedulerTimer != null)
			{
				TaskSchedulerTimer.Dispose();
				TaskSchedulerTimer = null;
			}

			if (TaskSchedulerTimer == null)
			{
				TaskSchedulerTimer = new Timer(e => {
					InBackgroundThread(action, "Task Scheduler");

					if (TaskSchedulerTimer != null)
					{
						TaskSchedulerTimer.Dispose();
						TaskSchedulerTimer = null;
					}
				}, null, delay, delay);
			}
		}

		public static bool IsSocketConnected(Socket s)
		{
			bool part1 = s.Poll(1000, SelectMode.SelectRead);
			bool part2 = s.Available == 0;
			if (part1 && part2)
			{
				return false;
			}

			return true;
		}

		public static void PlayNotification(NotificationContext context = NotificationContext.Normal, bool redirectOutput = false)
		{
			if (Core.IsUnknownOs)
			{
				Logger.Log("Cannot proceed as the running operating system is unknown.", LogLevels.Error);
				return;
			}

			if (Core.Config.MuteAssistant)
			{
				Logger.Log("Notifications are muted in config.", LogLevels.Trace);
				return;
			}

			if (!Directory.Exists(Constants.ResourcesDirectory))
			{
				Logger.Log("Resources directory doesn't exist!", LogLevels.Warn);
				return;
			}

			switch (context)
			{
				case NotificationContext.Imap:
					if (!File.Exists(Constants.IMAPPushNotificationFilePath))
					{
						Logger.Log("IMAP notification music file doesn't exist!", LogLevels.Warn);
						return;
					}

					ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.IMAPPushFileName} -q", Core.Config.Debug || redirectOutput);
					Logger.Log("Notification command processed sucessfully!", LogLevels.Trace);
					break;

				case NotificationContext.EmailSend:
					break;

				case NotificationContext.EmailSendFailed:
					break;

				case NotificationContext.FatalError:
					break;

				case NotificationContext.Normal:
					if (!Core.IsNetworkAvailable)
					{
						Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
					}
					break;
			}
		}

		public static string GetLocalIpAddress()
		{
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}

		public static ConsoleKeyInfo? FetchUserInputSingleChar(TimeSpan delay)
		{
			Task<ConsoleKeyInfo> task = Task.Factory.StartNew(Console.ReadKey);
			ConsoleKeyInfo? result = Task.WaitAny(new Task[] { task }, delay) == 0 ? task.Result : (ConsoleKeyInfo?) null;
			return result;
		}

		public static void SetConsoleTitle(string text) => Console.Title = $"{Core.AssistantName} Home Assistant V{Constants.Version} | {text}";

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}

		public static string GetExternalIp()
		{
			if (Core.IsNetworkAvailable)
			{
				string result = new WebClient().DownloadString("https://api.ipify.org/").Trim('\n');
				return result;
			}

			Logger.Log("No internet connection.", LogLevels.Error);
			return null;
		}

		internal static string TimeRan()
		{
			DateTime StartTime = Core.StartupTime;
			TimeSpan dt = DateTime.Now - StartTime;
			string Duration = "Online for: ";
			Duration += Math.Round(dt.TotalDays, 0) + " days, ";
			Duration += Math.Round(dt.TotalHours, 0) + " hours, ";
			Duration += Math.Round(dt.TotalMinutes, 0) + " minutes.";
			return Duration;
		}

		public static async Task DisplayAssistantASCII()
		{
			Logger.Log(@"  _______ ______  _____ _____ ", LogLevels.Ascii);
			await Task.Delay(200).ConfigureAwait(false);
			Logger.Log(@" |__   __|  ____|/ ____/ ____|", LogLevels.Ascii);
			await Task.Delay(200).ConfigureAwait(false);
			Logger.Log(@"    | |  | |__  | (___| (___  ", LogLevels.Ascii);
			await Task.Delay(200).ConfigureAwait(false);
			Logger.Log(@"    | |  |  __|  \___ \\___ \ ", LogLevels.Ascii);
			await Task.Delay(200).ConfigureAwait(false);
			Logger.Log(@"    | |  | |____ ____) |___) |", LogLevels.Ascii);
			await Task.Delay(200).ConfigureAwait(false);
			Logger.Log(@"    |_|  |______|_____/_____/ ", LogLevels.Ascii);
			await Task.Delay(100).ConfigureAwait(false);
			Logger.Log("\n", LogLevels.Ascii);
		}

		public static string FetchVariable(int arrayLine, bool returnParsed = false, string varName = null)
		{
			if (!File.Exists(Constants.VariablesPath))
			{
				Logger.Log("Variables file doesnt exist! aborting...", LogLevels.Error);
				return null;
			}

			string[] variables = File.ReadAllLines(Constants.VariablesPath);

			if (!string.IsNullOrEmpty(variables[arrayLine]) || !string.IsNullOrWhiteSpace(variables[arrayLine]))
			{
				if (returnParsed)
				{
					return ParseVariable(variables[arrayLine], varName);
				}

				return variables[arrayLine];
			}

			Logger.Log("Line is empty.", LogLevels.Error);
			return null;
		}

		private static string ParseVariable(string variableRaw, string variableName, char seperator = '=')
		{
			if (string.IsNullOrEmpty(variableRaw) || string.IsNullOrWhiteSpace(variableRaw))
			{
				Logger.Log("Variable is empty.", LogLevels.Error);
				return null;
			}

			if (string.IsNullOrEmpty(variableName) || string.IsNullOrWhiteSpace(variableName))
			{
				Logger.Log("Variable name is empty.", LogLevels.Error);
				return null;
			}

			string[] raw = variableRaw.Split(seperator);

			if (raw[0].Equals(variableName, StringComparison.OrdinalIgnoreCase))
			{
				return raw[1];
			}

			Logger.Log("Failed to parse variable.", LogLevels.Error);
			return null;
		}

		public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine) => Environment.GetEnvironmentVariable(variable, target);

		public static bool SetEnvironmentVariable(string variableName, string variableValue, EnvironmentVariableTarget target)
		{
			try
			{
				Environment.SetEnvironmentVariable(variableName, variableValue, target);
				return true;
			}
			catch (Exception e)
			{
				Logger.Log(e);
				return false;
			}
		}

		public static string GetUrlToString(string url, string Method = "GET", bool withuserAgent = true)
		{
			if (!Core.IsNetworkAvailable)
			{
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
				return null;
			}

			string Response = null;

			try
			{
				Uri address = new Uri(url);
				HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
				request.Method = Method;

				if (withuserAgent)
				{
					request.UserAgent = Constants.GitHubUserID;
				}

				request.ContentType = "text/xml";
				using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				{
					StreamReader reader = new StreamReader(response.GetResponseStream());
					Response = reader.ReadToEnd();
				}
			}
			catch (Exception e)
			{
				Logger.Log(e);
				return null;
			}

			return Response;
		}

		public static string GetUrlToString(string url, Method method, bool withuseragent = true)
		{
			if (!Core.IsNetworkAvailable)
			{
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
				return null;
			}

			if (url == null)
			{
				throw new ArgumentNullException(nameof(url));
			}

			try
			{
				RestClient client = new RestClient(url);
				RestRequest request = new RestRequest(method);

				if (withuseragent)
				{
					client.UserAgent = Constants.GitHubProjectName;
				}

				request.AddHeader("cache-control", "no-cache");
				IRestResponse response = client.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK)
				{
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
					return null;
				}

				return response.Content;
			}
			catch (PlatformNotSupportedException)
			{
				Logger.Log("Platform not supported exception during rest request.", LogLevels.Trace);
				return null;
			}
		}

		public static byte[] GetUrlToBytes(string url, Method method, string userAgent, string headerName = null, string headerValue = null)
		{
			if (!Core.IsNetworkAvailable)
			{
				Logger.Log("Cannot process, network is unavailable.", LogLevels.Warn);
				return new byte[0];
			}

			if (url == null)
			{
				throw new ArgumentNullException(nameof(url));
			}

			RestClient client = new RestClient(url);
			RestRequest request = new RestRequest(method);
			client.UserAgent = userAgent;
			request.AddHeader("cache-control", "no-cache");

			if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrEmpty(headerValue))
			{
				request.AddHeader(headerName, headerValue);
			}

			Logger.Log("Downloading bytes...", LogLevels.Trace);
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
				return null;
			}

			Logger.Log("Successfully downloaded", LogLevels.Trace);
			return response.RawBytes;
		}

		public static async Task<bool> RestartOrExit(bool Restart = false)
		{
			if (Restart)
			{
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

		public static string GetFileName(string path)
		{
			if (GetOsPlatform().Equals(OSPlatform.Windows))
			{
				return Path.GetFileName(path);
			}
			return path.Substring(path.LastIndexOf(FileSeperator, StringComparison.Ordinal) + 1);
		}

		public static string ReadLineMasked(char mask = '*')
		{
			StringBuilder result = new StringBuilder();

			ConsoleKeyInfo keyInfo;
			while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
			{
				if (!char.IsControl(keyInfo.KeyChar))
				{
					result.Append(keyInfo.KeyChar);
					Console.Write(mask);
				}
				else if ((keyInfo.Key == ConsoleKey.Backspace) && (result.Length > 0))
				{
					result.Remove(result.Length - 1, 1);

					if (Console.CursorLeft == 0)
					{
						Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
						Console.Write(' ');
						Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
					}
					else
					{

						// There are two \b characters here
						Console.Write(@" ");
					}
				}
			}

			Console.WriteLine();
			return result.ToString();
		}

		public static void WriteBytesToFile(byte[] bytesToWrite, string filePath)
		{
			if (bytesToWrite.Length <= 0 || string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath))
			{
				return;
			}

			File.WriteAllBytes(filePath, bytesToWrite);
		}

		public static (int, Thread) InBackgroundThread(Action action, string threadName, bool longRunning = false)
		{
			if (action == null)
			{
				Logger.Log("Action is null! " + nameof(action), LogLevels.Error);
				return (0, null);
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning)
			{
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = threadName;
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
			return (BackgroundThread.ManagedThreadId, BackgroundThread);
		}

		public static void InBackground(Action action, bool longRunning = false)
		{
			if (action == null)
			{
				Logger.Log("Action is null! " + nameof(action), LogLevels.Error);
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning)
			{
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static void ExecuteCommand(string command, bool redirectOutput = false, string fileName = "/bin/bash")
		{
			if (Core.IsUnknownOs && fileName == "/bin/bash")
			{
				Logger.Log($"{Core.AssistantName} is running on unknown OS. command cannot be executed.", LogLevels.Error);
				return;
			}

			try
			{
				Process proc = new Process
				{
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

				if (redirectOutput)
				{
					while (!proc.StandardOutput.EndOfStream)
					{
						Logger.Log(proc.StandardOutput.ReadLine(), LogLevels.Trace);
					}
				}
			}
			catch (PlatformNotSupportedException)
			{
				Logger.Log("Platform not supported exception thrown, internal error, cannot proceed.", LogLevels.Warn);
			}
			catch (Win32Exception)
			{
				Logger.Log("System cannot find the specified file.", LogLevels.Error);
			}
			catch (ObjectDisposedException)
			{
				Logger.Log("Object has been disposed already.", LogLevels.Error);
			}
			catch (InvalidOperationException)
			{
				Logger.Log("Invalid operation exception, internal error.", LogLevels.Error);
			}
		}

		public static void InBackground<T>(Func<T> function, bool longRunning = false)
		{
			if (function == null)
			{
				Logger.Log("Function is null! " + nameof(function), LogLevels.Error);
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning)
			{
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(function, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static async Task<IList<T>> InParallel<T>(IEnumerable<Task<T>> tasks)
		{
			if (tasks == null)
			{
				Logger.Log(nameof(tasks), LogLevels.Warn);
				return null;
			}

			IList<T> results = await Task.WhenAll(tasks).ConfigureAwait(false);
			return results;
		}

		public static bool IsRaspberryEnvironment()
		{
			if (Pi.Info.RaspberryPiVersion.ToString().Equals("Pi3ModelBEmbest", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return false;
		}

		public static async Task InParallel(IEnumerable<Task> tasks)
		{
			if (tasks == null)
			{
				Logger.Log(nameof(tasks), LogLevels.Warn);
				return;
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		public static bool CheckForInternetConnection()
		{
			try
			{
				Ping myPing = new Ping();
				string host = "8.8.8.8";
				byte[] buffer = new byte[32];
				int timeout = 1000;
				PingOptions pingOptions = new PingOptions();
				PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
				return reply != null && reply.Status == IPStatus.Success;
			}
			catch (Exception e)
			{
				Logger.Log(e);
				return false;
			}
		}

		public static bool CheckForInternetConnection(bool usingWebClient)
		{
			try
			{
				using (WebClient client = new WebClient())
				using (Stream stream = client.OpenRead("http://www.google.com"))
				{
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static void CloseProcess(string processName)
		{
			if (string.IsNullOrEmpty(processName) || string.IsNullOrWhiteSpace(processName))
			{
				return;
			}

			Process[] workers = Process.GetProcessesByName(processName);
			foreach (Process worker in workers)
			{
				worker.Kill();
				Logger.Log($"Closed {processName} process.");
				worker.WaitForExit();
				worker.Dispose();
			}
		}

		public static bool IsNullOrEmpty(string value) => string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);

		public static void CheckMultipleProcess()
		{
			string RunningProcess = Process.GetCurrentProcess().ProcessName;
			Process[] processes = Process.GetProcessesByName(RunningProcess);
			if (processes.Length > 1)
			{
				Logger.Log("Exiting current process as another instance of same application is running...");
				Process.GetCurrentProcess().Kill();
			}
		}
	}
}
