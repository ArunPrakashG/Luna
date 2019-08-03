using Assistant.Extensions;
using Assistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.AssistantCore {

	public class CoreConfig : IEquatable<CoreConfig> {

		[JsonProperty] public bool AutoRestart { get; set; } = false;

		[JsonProperty] public bool AutoUpdates { get; set; } = true;

		[JsonProperty] public bool EnableConfigWatcher { get; set; } = true;

		[JsonProperty] public bool EnableModuleWatcher { get; set; } = true;

		[JsonProperty] public bool EnableModules { get; set; } = true;

		[JsonProperty] public int UpdateIntervalInHours { get; set; } = 5;

		[JsonProperty] public string KestrelServerUrl { get; set; } = "http://localhost:9090";

		[JsonProperty] public int ServerAuthCode { get; set; } = 3033;

		[JsonProperty] public bool PushBulletLogging { get; set; } = true;

		[JsonProperty] public int TCPServerPort { get; set; } = 6060;

		[JsonProperty] public bool TCPServer { get; set; } = true;

		[JsonProperty] public bool KestrelServer { get; set; } = true;

		[JsonProperty] public bool GPIOSafeMode { get; set; } = false;

		[JsonProperty] public List<string> VerifiedAuthenticationTokens { get; set; }

		[JsonProperty]
		public int[] RelayPins = new int[]
		{
			2, 3, 4, 17, 27, 22, 10, 9
		};

		[JsonProperty]
		public int[] IRSensorPins = new int[] {
			24
		};

		[JsonProperty] public bool DisplayStartupMenu { get; set; } = false;

		[JsonProperty] public bool EnableGpioControl { get; set; } = true;

		[JsonProperty] public bool Debug { get; set; } = false;

		[JsonProperty] public string OwnerEmailAddress { get; set; } = "arun.prakash.456789@gmail.com";

		[JsonProperty] public bool EnableFirstChanceLog { get; set; } = false;

		[JsonProperty] public bool EnableTextToSpeech { get; set; } = true;

		[JsonProperty] public bool MuteAssistant { get; set; } = false;

		[JsonProperty] public string OpenWeatherApiKey { get; set; } = null;

		[JsonProperty] public string GitHubToken { get; set; }

		[JsonProperty] public string PushBulletApiKey { get; set; } = null;

		[JsonProperty] public string AssistantEmailId { get; set; }

		[JsonProperty] public string AssistantDisplayName { get; set; } = "TESS";

		[JsonProperty] public string AssistantEmailPassword { get; set; }

		[JsonProperty(Required = Required.Default)] public DateTime ProgramLastStartup { get; set; }

		[JsonProperty(Required = Required.Default)] public DateTime ProgramLastShutdown { get; set; }

		[JsonProperty] public bool CloseRelayOnShutdown { get; set; } = false;

		[JsonIgnore] private readonly Logger Logger = new Logger("CORE-CONFIG");

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

			Logger.Log(eventRaisedByConfigWatcher ? "Updated core config!" : "Core Configuration Loaded Successfully!", Enums.LogLevels.Trace);

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
						Task.Run(async () => await Core.Exit().ConfigureAwait(false));
						return false;

					default:
						Logger.Log("Unknown value entered! continuing to run the Core...");
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
				hashCode = (hashCode * 397) ^ UpdateIntervalInHours;
				hashCode = (hashCode * 397) ^ TCPServer.GetHashCode();
				hashCode = (hashCode * 397) ^ KestrelServer.GetHashCode();
				hashCode = (hashCode * 397) ^ GPIOSafeMode.GetHashCode();
				hashCode = (hashCode * 397) ^ (RelayPins != null ? RelayPins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (IRSensorPins != null ? IRSensorPins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ DisplayStartupMenu.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableGpioControl.GetHashCode();
				hashCode = (hashCode * 397) ^ Debug.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableFirstChanceLog.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableTextToSpeech.GetHashCode();
				hashCode = (hashCode * 397) ^ MuteAssistant.GetHashCode();
				hashCode = (hashCode * 397) ^ CloseRelayOnShutdown.GetHashCode();
				hashCode = (hashCode * 397) ^ ServerAuthCode;
				hashCode = (hashCode * 397) ^ TCPServerPort;
				hashCode = (hashCode * 397) ^ (OwnerEmailAddress != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerEmailAddress) : 0);
				hashCode = (hashCode * 397) ^ (AssistantEmailId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(AssistantEmailId) : 0);
				hashCode = (hashCode * 397) ^ (AssistantEmailPassword != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(AssistantEmailPassword) : 0);
				hashCode = (hashCode * 397) ^ ProgramLastStartup.GetHashCode();
				hashCode = (hashCode * 397) ^ ProgramLastShutdown.GetHashCode();
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
				   UpdateIntervalInHours == other.UpdateIntervalInHours && TCPServer == other.TCPServer &&
				   KestrelServer == other.KestrelServer && GPIOSafeMode == other.GPIOSafeMode &&
				   Equals(RelayPins, other.RelayPins) && Equals(IRSensorPins, other.IRSensorPins) &&
				   DisplayStartupMenu == other.DisplayStartupMenu && EnableGpioControl == other.EnableGpioControl &&
				   Debug == other.Debug && EnableFirstChanceLog == other.EnableFirstChanceLog &&
				   EnableTextToSpeech == other.EnableTextToSpeech && MuteAssistant == other.MuteAssistant &&
				   CloseRelayOnShutdown == other.CloseRelayOnShutdown && ServerAuthCode == other.ServerAuthCode &&
				   TCPServerPort == other.TCPServerPort &&
				   string.Equals(OwnerEmailAddress, other.OwnerEmailAddress, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(AssistantEmailId, other.AssistantEmailId, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(AssistantEmailPassword, other.AssistantEmailPassword, StringComparison.OrdinalIgnoreCase) &&
				   ProgramLastStartup.Equals(other.ProgramLastStartup) &&
				   ProgramLastShutdown.Equals(other.ProgramLastShutdown);
		}

		public static bool operator ==(CoreConfig left, CoreConfig right) => Equals(left, right);

		public static bool operator !=(CoreConfig left, CoreConfig right) => !Equals(left, right);
	}
}
