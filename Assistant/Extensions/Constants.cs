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

		public const string GPIOConfigPath = ConfigDirectory + "/GpioConfig.json";
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
		public const string GitHubProjectName = nameof(Assistant);
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

		public const string StartupFileName = "startup.mp3";
		public const string NewMailFileName = "newmail.mp3";
		public const string ShutdownFileName = "shutdown.mp3";
		public const string IMAPPushFileName = "mail_push_notification.mp3";
		public const string TTSAlertFileName = "tts_alert.mp3";

		public const char ConsoleCommandMenuKey = 'c';
		public const char ConsoleDelayedShutdownKey = 'q';
		public const char ConsoleQuickShutdownKey = 'e';
		public const char ConsoleRelayCommandMenuKey = 'g';
		public const char ConsoleTestMethodExecutionKey = 't';
		public const char ConsoleModuleShutdownKey = 'm';
		public const char ConsoleRelayCycleMenuKey = 'r';

		public static string ExternelIP { get; set; }

		public static string LocalIP { get; set; }
		public const string GitHubReleaseURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/latest";
		public const string GitHubAssetDownloadURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/assets/";

		public static string HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		public static Guid ModuleVersion => Assembly.GetEntryAssembly().ManifestModule.ModuleVersionId;

		public static Version Version => Assembly.GetEntryAssembly().GetName().Version;
	}
}
