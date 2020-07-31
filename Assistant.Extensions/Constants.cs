using System;
using System.IO;
using System.Reflection;

namespace Luna.ExternalExtensions {
	public static class Constants {
		

		public const string GpioConfigDirectory = ConfigDirectory + "/" + GpioConfigFile;
		public const string CoreConfigPath = ConfigDirectory + "/Assistant.json";
		public const string IPBlacklistPath = ConfigDirectory + "/IPBlacklist.txt";
		public const string TaskQueueFilePath = ConfigDirectory + "/TaskQueue.json";
		public const string KestrelConfigurationFile = "KestrelServer" + ".config";
		public const string KestrelConfigFilePath = ConfigDirectory + "/" + KestrelConfigurationFile;

		public const string BackupDirectoryPath = @"_old";
		public const string UpdateZipFileName = @"Latest.zip";
		
		public const string VariablesPath = "Variables.txt";
		
		public const string AssistantPushBulletChannelName = "assistantcontroller";
		public const string GmailHost = "imap.gmail.com";
		public const string SMTPHost = "smtp.mailserver.com";
		public const int SMPTPort = 465;
		public const int GmailPort = 993;
		

		public const string StartupSpeechFilePath = TextToSpeechDirectory + "/startup.mp3";
		public const string NewMailSpeechFilePath = TextToSpeechDirectory + "/newmail.mp3";
		public const string ShutdownSpeechFilePath = TextToSpeechDirectory + "/shutdown.mp3";
		public const string IMAPPushNotificationFilePath = ResourcesDirectory + "/mail_push_notification.mp3";
		public const string TTSAlertFilePath = ResourcesDirectory + "/tts_alert.mp3";
		public const string AlarmFilePath = ResourcesDirectory + "/" + AlarmFileName;
		public const string ALERT_SOUND_PATH = ResourcesDirectory + "/" + "alert.mp3";

		public const string StartupFileName = "startup.mp3";
		public const string NewMailFileName = "newmail.mp3";
		public const string ShutdownFileName = "shutdown.mp3";
		public const string IMAPPushFileName = "mail_push_notification.mp3";
		public const string TTSAlertFileName = "tts_alert.mp3";
		public const string AlarmFileName = "alarm.mp3";

		public const ConsoleKey SHELL_KEY = ConsoleKey.C;
		public const char ConsoleShutdownKey = 'q';
		public const char ConsoleQuickShutdownKey = 'e';
		public const char ConsoleRelayCommandMenuKey = 'g';
		public const char ConsoleTestMethodExecutionKey = 't';
		public const char ConsoleModuleShutdownKey = 'm';
		public const char ConsoleRelayCycleMenuKey = 'r';
		public const char ConsoleMorseCodeKey = 'v';
		public const char ConsoleWeatherInfoKey = 'w';

		public static string? ExternelIP { get; set; } = string.Empty;

		public static string? LocalIP { get; set; }
		
	}
}
