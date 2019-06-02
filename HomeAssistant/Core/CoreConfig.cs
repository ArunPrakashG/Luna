using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace HomeAssistant.Core {

	public class CoreConfig {

		[JsonProperty]
		public bool AutoRestart = false;

		[JsonProperty]
		public bool AutoUpdates = true;

		[JsonProperty]
		public int ServerAuthCode { get; set; } = 3033;

		[JsonProperty]
		public int ServerPort { get; set; } = 6060;

		[JsonProperty]
		public bool TCPServer = true;

		[JsonProperty]
		public bool GPIOSafeMode = false;

		[JsonProperty]
		public int[] RelayPins = new int[]
		{
			2, 3, 4, 17, 27, 22, 10, 9
		};

		[JsonProperty]
		public int[] IRSensorPins = new int[] {
			12
		};

		[JsonProperty]
		public bool DisplayStartupMenu = false;

		[JsonProperty]
		public bool GPIOControl = true;

		[JsonProperty]
		public bool Debug = false;

		[JsonProperty]
		public bool EmailNotifications = false;

		[JsonProperty]
		public ConcurrentDictionary<string, string> EmailDetails = new ConcurrentDictionary<string, string>();

		[JsonProperty]
		public ConcurrentDictionary<string, string> SendFromEmails = new ConcurrentDictionary<string, string>();

		[JsonProperty]
		public DateTime ProgramLastStartup { get; set; }

		[JsonProperty]
		public DateTime ProgramLastShutdown { get; set; }

		[JsonProperty]
		public ulong DiscordOwnerID { get; set; } = 161859532920848384;

		[JsonProperty]
		public ulong DiscordServerID { get; set; } = 580995322369802240;

		[JsonProperty]
		public ulong DiscordLogChannelID { get; set; } = 580995512526831616;

		[JsonProperty]
		public bool DiscordLog = true;

		[JsonProperty]
		public bool DiscordBot = true;

		[JsonProperty]
		public bool ImapNotifications = true;

		[JsonProperty]
		public bool CloseRelayOnShutdown = false;

		[JsonIgnore]
		private Logger Logger = new Logger("CORE-CONFIG");

		public void SaveConfig(CoreConfig config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(config, Formatting.Indented);
			string pathName = Constants.CoreConfigPath;
			using (StreamWriter sw = new StreamWriter(pathName, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, config);
					Logger.Log("Updated Core Config!");
					sw.Dispose();
				}
			}
		}

		public CoreConfig LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.CoreConfigPath)) {
				if (!GenerateDefaultConfig()) {
					return null;
				}
			}

			string JSON = null;
			using (FileStream Stream = new FileStream(Constants.CoreConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			CoreConfig returnConfig = JsonConvert.DeserializeObject<CoreConfig>(JSON);

			Logger.Log("Core Configuration Loaded Successfully!");
			return returnConfig;
		}

		public bool GenerateDefaultConfig() {
			Logger.Log("Core config file doesnt exist. press c to continue generating default config or q to quit.");
			Task waitTask = Task.Delay(60000);

			Task<ConsoleKeyInfo> inputTask = Task.Run(() => {
				return Console.ReadKey();
			});

			Task.WhenAny(new[] { waitTask, inputTask });

			switch (inputTask.Result.KeyChar) {
				case 'c':
					break;

				case 'q':
					Task.Run(async () => await Program.Exit(0).ConfigureAwait(false));
					return false;

				default:
					Logger.Log("Unknown value entered! continuing to run the program...");
					break;
			}

			Logger.Log("Generating default Config...");
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesnt exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (File.Exists(Constants.CoreConfigPath)) {
				return true;
			}

			CoreConfig Config = new CoreConfig();
			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(Config, Formatting.Indented);
			string pathName = Constants.CoreConfigPath;
			using (StreamWriter sw = new StreamWriter(pathName, false))
			using (JsonWriter writer = new JsonTextWriter(sw)) {
				writer.Formatting = Formatting.Indented;
				serializer.Serialize(writer, Config);
				sw.Dispose();
			}
			return true;
		}
	}
}
