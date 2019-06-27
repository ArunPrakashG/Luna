using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using HomeAssistant.Modules.Interfaces;

namespace HomeAssistant.Core {
	

	public class CoreConfig : IEquatable<CoreConfig> {

		[JsonProperty]
		public bool AutoRestart = false;

		[JsonProperty]
		public bool AutoUpdates = true;

		[JsonProperty]
		public bool EnableConfigWatcher = true;

		[JsonProperty]
		public bool EnableModuleWatcher = true;

		[JsonProperty]
		public bool EnableImapIdleWorkaround = true;

		[JsonProperty]
		public int UpdateIntervalInHours = 5;

		[JsonProperty]
		public bool ListernLocalHostOnly = false;

		[JsonProperty]
		public int ServerAuthCode { get; set; } = 3033;

		[JsonProperty]
		public int ServerPort { get; set; } = 6060;

		[JsonProperty]
		public bool TCPServer = true;

		[JsonProperty]
		public bool KestrelServer = true;

		[JsonProperty]
		public int KestrelServerPort { get; set; } = 9090;

		[JsonProperty]
		public bool GPIOSafeMode = false;

		[JsonProperty]
		public int[] RelayPins = new int[]
		{
			2, 3, 4, 17, 27, 22, 10, 9
		};

		[JsonProperty]
		public int[] IRSensorPins = new int[] {
			24
		};

		[JsonProperty]
		public bool DisplayStartupMenu = false;

		[JsonProperty]
		public bool GPIOControl = true;

		[JsonProperty]
		public bool Debug = false;

		[JsonProperty]
		public string OwnerEmailAddress { get; set; }

		[JsonProperty]
		public bool EnableFirstChanceLog = false;

		[JsonProperty]
		public bool EnableTextToSpeech = true;

		[JsonProperty]
		public bool MuteAll = false;

		[JsonProperty]
		public bool EnableEmail = true;

		[JsonProperty]
		public bool EnableGoogleMap = true;

		[JsonProperty]
		public bool EnableYoutube = true;

		[JsonProperty]
		public string TessEmailID { get; set; }

		[JsonProperty]
		public string TessEmailPASS { get; set; }

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
		public bool CloseRelayOnShutdown = false;

		[JsonIgnore]
		private readonly Logger Logger = new Logger("CORE-CONFIG");

		public CoreConfig SaveConfig(CoreConfig config) {
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
					return config;
				}
			}
		}

		public CoreConfig LoadConfig(bool eventRaisedByConfigWatcher = false) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!eventRaisedByConfigWatcher && !File.Exists(Constants.CoreConfigPath)) {
				if (!GenerateDefaultConfig()) {
					return null;
				}
			}

			string JSON;
			using (FileStream Stream = new FileStream(Constants.CoreConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			CoreConfig returnConfig = JsonConvert.DeserializeObject<CoreConfig>(JSON);

			Logger.Log(eventRaisedByConfigWatcher ? "Updated core config!" : "Core Configuration Loaded Successfully!");

			return returnConfig;
		}

		public bool GenerateDefaultConfig() {
			Logger.Log("Core config file doesnt exist. press c to continue generating default config or q to quit.");

			ConsoleKeyInfo? Key = Helpers.FetchUserInputSingleChar(TimeSpan.FromMinutes(1));

			if (!Key.HasValue) {
				Logger.Log("No value has been entered, continuing to run the program...");
			}
			else {
				switch (Key.Value.KeyChar) {
					case 'c':
						break;

					case 'q':
						Task.Run(async () => await Tess.Exit().ConfigureAwait(false));
						return false;

					default:
						Logger.Log("Unknown value entered! continuing to run the Tess...");
						break;
				}
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

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj.GetType() != this.GetType()) {
				return false;
			}

			return Equals((CoreConfig) obj);
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = AutoRestart.GetHashCode();
				hashCode = (hashCode * 397) ^ AutoUpdates.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableConfigWatcher.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableImapIdleWorkaround.GetHashCode();
				hashCode = (hashCode * 397) ^ UpdateIntervalInHours;
				hashCode = (hashCode * 397) ^ TCPServer.GetHashCode();
				hashCode = (hashCode * 397) ^ KestrelServer.GetHashCode();
				hashCode = (hashCode * 397) ^ GPIOSafeMode.GetHashCode();
				hashCode = (hashCode * 397) ^ (RelayPins != null ? RelayPins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (IRSensorPins != null ? IRSensorPins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ DisplayStartupMenu.GetHashCode();
				hashCode = (hashCode * 397) ^ GPIOControl.GetHashCode();
				hashCode = (hashCode * 397) ^ Debug.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableFirstChanceLog.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableTextToSpeech.GetHashCode();
				hashCode = (hashCode * 397) ^ MuteAll.GetHashCode();
				hashCode = (hashCode * 397) ^ DiscordLog.GetHashCode();
				hashCode = (hashCode * 397) ^ DiscordBot.GetHashCode();
				hashCode = (hashCode * 397) ^ CloseRelayOnShutdown.GetHashCode();
				hashCode = (hashCode * 397) ^ ServerAuthCode;
				hashCode = (hashCode * 397) ^ ServerPort;
				hashCode = (hashCode * 397) ^ KestrelServerPort;
				hashCode = (hashCode * 397) ^ (OwnerEmailAddress != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerEmailAddress) : 0);
				hashCode = (hashCode * 397) ^ (TessEmailID != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(TessEmailID) : 0);
				hashCode = (hashCode * 397) ^ (TessEmailPASS != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(TessEmailPASS) : 0);
				hashCode = (hashCode * 397) ^ ProgramLastStartup.GetHashCode();
				hashCode = (hashCode * 397) ^ ProgramLastShutdown.GetHashCode();
				hashCode = (hashCode * 397) ^ DiscordOwnerID.GetHashCode();
				hashCode = (hashCode * 397) ^ DiscordServerID.GetHashCode();
				hashCode = (hashCode * 397) ^ DiscordLogChannelID.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString() => base.ToString();

		public bool Equals(CoreConfig other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return AutoRestart == other.AutoRestart && AutoUpdates == other.AutoUpdates &&
				   EnableConfigWatcher == other.EnableConfigWatcher &&
				   EnableImapIdleWorkaround == other.EnableImapIdleWorkaround &&
				   UpdateIntervalInHours == other.UpdateIntervalInHours && TCPServer == other.TCPServer &&
				   KestrelServer == other.KestrelServer && GPIOSafeMode == other.GPIOSafeMode &&
				   Equals(RelayPins, other.RelayPins) && Equals(IRSensorPins, other.IRSensorPins) &&
				   DisplayStartupMenu == other.DisplayStartupMenu && GPIOControl == other.GPIOControl &&
				   Debug == other.Debug && EnableFirstChanceLog == other.EnableFirstChanceLog &&
				   EnableTextToSpeech == other.EnableTextToSpeech && MuteAll == other.MuteAll &&
				   DiscordLog == other.DiscordLog && DiscordBot == other.DiscordBot &&
				   CloseRelayOnShutdown == other.CloseRelayOnShutdown && ServerAuthCode == other.ServerAuthCode &&
				   ServerPort == other.ServerPort && KestrelServerPort == other.KestrelServerPort &&
				   string.Equals(OwnerEmailAddress, other.OwnerEmailAddress, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(TessEmailID, other.TessEmailID, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(TessEmailPASS, other.TessEmailPASS, StringComparison.OrdinalIgnoreCase) &&
				   ProgramLastStartup.Equals(other.ProgramLastStartup) &&
				   ProgramLastShutdown.Equals(other.ProgramLastShutdown) && DiscordOwnerID == other.DiscordOwnerID &&
				   DiscordServerID == other.DiscordServerID && DiscordLogChannelID == other.DiscordLogChannelID;
		}

		public static bool operator ==(CoreConfig left, CoreConfig right) => Equals(left, right);

		public static bool operator !=(CoreConfig left, CoreConfig right) => !Equals(left, right);

	}
}
