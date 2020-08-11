using System;
using System.IO;
using System.Reflection;

namespace Luna {
	public static class Constants {
		#region Internals
		public const string ConfigDirectory = "Config";
		public const string ModuleDirectory = "Modules";
		public const string CommandsDirectory = "Commands";
		public const string GpioConfig = "GpioConfig.json";
		public const string CoreConfig = "Luna.json";
		public static readonly string GpioConfigPath;
		public static readonly string CoreConfigPath;
		#endregion

		#region GitHub
		public const string GitAPI = "https://api.github.com/repos/";
		public const string GitUserID = "ArunPrakashG";
		public const string GitProjectName = "Luna";
		public const string GitReleaseUrl = GitAPI + GitUserID + "/" + GitProjectName + "/releases/latest";
		public const string GitDownloadUrl = GitAPI + GitUserID + "/" + GitProjectName + "/releases/assets/";
		#endregion

		#region Gpio
		public static readonly int[] BcmGpioPins = new int[26] {
			2,3,4,17,27,22,10,9,11,5,6,13,19,26,14,15,18,23,24,25,8,7,12,16,20,21
		};
		#endregion

		#region GeneralInternal
		public static readonly string HomeDirectory;
		public static readonly Version Version;
		#endregion

		static Constants() {
			GpioConfigPath = Path.Combine(ConfigDirectory, GpioConfig);
			CoreConfigPath = Path.Combine(ConfigDirectory, CoreConfig);
			HomeDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Assembly.GetExecutingAssembly().Location;
			Version = Assembly.GetEntryAssembly()?.GetName().Version ?? Assembly.GetExecutingAssembly().GetName().Version ?? throw new NullReferenceException(nameof(Version));
		}
	}
}
