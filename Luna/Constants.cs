using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Luna {
	internal class Constants {
		internal const string TraceLogPath = "TraceLog.txt";
		internal const string DebugLogPath = "DebugLog.txt";
		internal const string GpioConfigFile = "GpioConfig.json";
		internal const string ConfigDirectory = "Config";
		internal const string ResourcesDirectory = "Resources";
		internal const string ModuleDirectory = "Modules";
		internal const string COMMANDS_PATH = "Commands";
		internal const string TextToSpeechDirectory = ResourcesDirectory + "/TTS";
		internal const string GitHubAPI = "https://api.github.com/repos/";
		internal const string GitHubUserID = "ArunPrakashG";
		internal const string GitHubProjectName = "HomeAssistant";
		internal const string GITHUB_RELEASE_URL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/latest";
		internal const string GitHubAssetDownloadURL = GitHubAPI + GitHubUserID + "/" + GitHubProjectName + "/releases/assets/";

		internal static readonly int[] BcmGpioPins = new int[26] {
			2,3,4,17,27,22,10,9,11,5,6,13,19,26,14,15,18,23,24,25,8,7,12,16,20,21
		};

		internal static string? HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

		internal static Version? Version => Assembly.GetEntryAssembly()?.GetName().Version;
	}
}
