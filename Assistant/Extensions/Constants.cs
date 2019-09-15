
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

using System;
using System.IO;
using System.Reflection;
using Assistant.Server;

namespace Assistant.Extensions {

	public static class Constants {
		public const string TraceLogPath = @"TraceLog.txt";
		public const string DebugLogPath = @"DebugLog.txt";
		public const string ConfigDirectory = @"Config";
		public const string ResourcesDirectory = @"Resources";
		public const string ModuleDirectory = @"Modules";
		public const string TextToSpeechDirectory = ResourcesDirectory + "/TTS";

		public const string GpioConfigPath = ConfigDirectory + "/GpioConfig.json";
		public const string CoreConfigPath = ConfigDirectory + "/Assistant.json";
		public const string IPBlacklistPath = ConfigDirectory + "/IPBlacklist.txt";
		public const string TaskQueueFilePath = ConfigDirectory + "/TaskQueue.json";
		public const string KestrelConfigurationFile = nameof(KestrelServer) + ".config";
		public const string KestrelConfigFilePath = ConfigDirectory + "/" + KestrelConfigurationFile;

		[Obsolete("Using temporarily")]
		public const int KestrelAuthCode = 3033;

		public const string BackupDirectoryPath = @"_old";
		public const string UpdateZipFileName = @"Latest.zip";
		public const string GitHubUserID = "SynergyFTW";
		public const string VariablesPath = "Variables.txt";
		public const string GitHubProjectName = "HomeAssistant";
		public const string AssistantPushBulletChannelName = "assistantcontroller";
		public const string GmailHost = "imap.gmail.com";
		public const string SMTPHost = "smtp.mailserver.com";
		public const int SMPTPort = 465;
		public const int GmailPort = 993;
		public const string GitHubAPI = "https://api.github.com/repos/";

		public const string StartupSpeechFilePath = TextToSpeechDirectory + "/startup.mp3";
		public const string NewMailSpeechFilePath = TextToSpeechDirectory + "/newmail.mp3";
		public const string ShutdownSpeechFilePath = TextToSpeechDirectory + "/shutdown.mp3";
		public const string IMAPPushNotificationFilePath = ResourcesDirectory + "/mail_push_notification.mp3";
		public const string TTSAlertFilePath = ResourcesDirectory + "/tts_alert.mp3";
		public const string AlarmFilePath = ResourcesDirectory + "/" + AlarmFileName;

		public const string StartupFileName = "startup.mp3";
		public const string NewMailFileName = "newmail.mp3";
		public const string ShutdownFileName = "shutdown.mp3";
		public const string IMAPPushFileName = "mail_push_notification.mp3";
		public const string TTSAlertFileName = "tts_alert.mp3";
		public const string AlarmFileName = "alarm.mp3";

		public const char ConsoleCommandMenuKey = 'c';
		public const char ConsoleDelayedShutdownKey = 'q';
		public const char ConsoleQuickShutdownKey = 'e';
		public const char ConsoleRelayCommandMenuKey = 'g';
		public const char ConsoleTestMethodExecutionKey = 't';
		public const char ConsoleModuleShutdownKey = 'm';
		public const char ConsoleRelayCycleMenuKey = 'r';
		public const char ConsoleMorseCodeKey = 'v';
		public const char ConsoleWheatherInfoKey = 'w';

		public static string ExternelIP { get; set; }

		public static string LocalIP { get; set; }
		public const string GitHubReleaseURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/latest";
		public const string GitHubAssetDownloadURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/assets/";

		public static readonly int[] BcmGpioPins = new int[26] {
			2,3,4,17,27,22,10,9,11,5,6,13,19,26,14,15,18,23,24,25,8,7,12,16,20,21
		};

		public static string HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		public static Guid ModuleVersion => Assembly.GetEntryAssembly().ManifestModule.ModuleVersionId;
		public static Version Version => Assembly.GetEntryAssembly().GetName().Version;
	}
}
