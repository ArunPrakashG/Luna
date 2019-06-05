using System;
using System.IO;
using System.Reflection;

namespace HomeAssistant.Extensions {

	public static class Constants {
		public const string TraceLogPath = @"TraceLog.txt";
		public const string DebugLogPath = @"DebugLog.txt";
		public const string ConfigDirectory = @"Config";
		public const string ResourcesDirectory = @"Resources";
		public const string TextToSpeechDirectory = @"TTS";
		public const string IMAPSoundName = "notifications.mp3";
		public const string GPIOConfigPath = ConfigDirectory + "/GPIOConfig.json";
		public const string CoreConfigPath = ConfigDirectory + "/TESS.json";
		public const string IPBlacklistPath = ConfigDirectory + "/IPBlacklist.txt";
		public const string BackupDirectoryPath = @"_old";
		public const string UpdateZipFileName = @"Latest.zip";
		public const string GitHubUserID = "SynergyFTW";
		public const string VariablesPath = "Variables.txt";
		public const string GitHubProjectName = nameof(HomeAssistant);
		public const string GmailHost = "imap.gmail.com";
		public const string SMTPHost = "smtp.mailserver.com";
		public const int SMPTPort = 465;
		public const int GmailPort = 993;
		public const string GitHubAPI = "https://api.github.com/repos/";

		public static string ExternelIP { get; set; }
		public const string GitHubReleaseURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/latest";
		public const string GitHubAssetDownloadURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/assets/";

		public static string HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		public static Guid ModuleVersion => Assembly.GetEntryAssembly().ManifestModule.ModuleVersionId;

		public static Version Version => Assembly.GetEntryAssembly().GetName().Version;
	}
}
