using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;

namespace Assistant.Core {
	public class CoreConfig : IEquatable<CoreConfig> {
		[JsonProperty] public bool AutoRestart { get; set; } = false;

		[JsonProperty] public bool AutoUpdates { get; set; } = true;

		[JsonProperty] public bool EnableConfigWatcher { get; set; } = true;

		[JsonProperty] public bool EnableModules { get; set; } = true;

		[JsonProperty] public int UpdateIntervalInHours { get; set; } = 5;

		[JsonProperty] public int ServerAuthCode { get; set; } = 3033;

		[JsonProperty] public bool GpioSafeMode { get; set; } = false;

		[JsonProperty]
		public int[] OutputModePins = new int[]
		{
			2, 3, 4, 17, 27, 22, 10, 9
		};

		[JsonProperty]
		public int[] InputModePins = new int[] {
			26,20,16
		};

		[JsonProperty]
		public int[] IRSensorPins = new int[] {
			26,20
		};

		[JsonProperty]
		public int[] SoundSensorPins = new int[] {
			16
		};

		[JsonProperty]
		public int[] RelayPins = new int[] {
			2, 3, 4, 17, 27, 22, 10, 9
		};

		[JsonProperty] public bool DisplayStartupMenu { get; set; } = false;

		[JsonProperty] public bool EnableGpioControl { get; set; } = true;

		[JsonProperty] public bool Debug { get; set; } = false;

		[JsonProperty] public string? StatisticsServerIP { get; set; }

		[JsonProperty] public string? OwnerEmailAddress { get; set; } = "arun.prakash.456789@gmail.com";

		[JsonProperty] public bool EnableFirstChanceLog { get; set; } = false;

		[JsonProperty] public bool EnableTextToSpeech { get; set; } = true;

		[JsonProperty] public bool MuteAssistant { get; set; } = false;

		[JsonProperty] public string? OpenWeatherApiKey { get; set; }

		[JsonProperty] public string? PushBulletApiKey { get; set; }

		[JsonProperty] public string? AssistantEmailId { get; set; }

		[JsonProperty] public string AssistantDisplayName { get; set; } = "Home Assistant";

		[JsonProperty] public string? AssistantEmailPassword { get; set; }

		[JsonProperty(Required = Required.Default)] public DateTime ProgramLastStartup { get; set; }

		[JsonProperty(Required = Required.Default)] public DateTime ProgramLastShutdown { get; set; }

		[JsonProperty] public bool CloseRelayOnShutdown { get; set; } = false;

		private static readonly ILogger Logger = new Logger(typeof(CoreConfig).Name);
		private static readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);

		public async Task<CoreConfig?> SaveConfig(CoreConfig config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			Logger.Log("Saving core config...", LogLevels.Trace);
			await ConfigSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				string filePath = Constants.CoreConfigPath;
				string json = JsonConvert.SerializeObject(config, Formatting.Indented);
				string newFilePath = filePath + ".new";

				Core.FileWatcher.IsOnline = false;
				File.WriteAllText(newFilePath, json);

				if (File.Exists(filePath)) {
					File.Replace(newFilePath, filePath, null);
				}
				else {
					File.Move(newFilePath, filePath);
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return null;
			}
			finally {
				ConfigSemaphore.Release();
				Core.FileWatcher.IsOnline = true;
			}

			Logger.Log("Saved core config!", LogLevels.Trace);
			return config;
		}

		public async Task LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.CoreConfigPath) && !GenerateDefaultConfig()) {
				return;
			}

			await ConfigSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				Logger.Log("Loading core config...", LogLevels.Trace);					
				using StreamReader streamReader = new StreamReader(new FileStream(Constants.CoreConfigPath, FileMode.Open, FileAccess.Read));
				Core.Config = JsonConvert.DeserializeObject<CoreConfig>(streamReader.ReadToEnd());
				Logger.Log("Core configuration loaded successfully!", LogLevels.Trace);
			}
			catch (Exception e) {
				Logger.Log(e);
			}
			finally {
				ConfigSemaphore.Release();
			}
		}

		public bool GenerateDefaultConfig() {
			Logger.Log("Core config file doesn't exist. press c to continue generating default config or q to quit.");

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
				Logger.Log("Config directory doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (File.Exists(Constants.CoreConfigPath)) {
				return true;
			}

			CoreConfig Config = new CoreConfig();
			Helpers.InBackgroundThread(async () => await SaveConfig(new CoreConfig()).ConfigureAwait(false));
			return true;
		}

		public override bool Equals(object? obj) {
			if (obj is null) {
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
				hashCode = (hashCode * 397) ^ GpioSafeMode.GetHashCode();
				hashCode = (hashCode * 397) ^ (OutputModePins != null ? OutputModePins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (InputModePins != null ? InputModePins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ DisplayStartupMenu.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableGpioControl.GetHashCode();
				hashCode = (hashCode * 397) ^ Debug.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableFirstChanceLog.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableTextToSpeech.GetHashCode();
				hashCode = (hashCode * 397) ^ MuteAssistant.GetHashCode();
				hashCode = (hashCode * 397) ^ CloseRelayOnShutdown.GetHashCode();
				hashCode = (hashCode * 397) ^ ServerAuthCode;
				hashCode = (hashCode * 397) ^ (OwnerEmailAddress != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerEmailAddress) : 0);
				hashCode = (hashCode * 397) ^ (AssistantEmailId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(AssistantEmailId) : 0);
				hashCode = (hashCode * 397) ^ (AssistantEmailPassword != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(AssistantEmailPassword) : 0);
				hashCode = (hashCode * 397) ^ ProgramLastStartup.GetHashCode();
				hashCode = (hashCode * 397) ^ ProgramLastShutdown.GetHashCode();
				return hashCode;
			}
		}

		public override string? ToString() => base.ToString();

		public bool Equals(CoreConfig other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return AutoRestart == other.AutoRestart && AutoUpdates == other.AutoUpdates &&
				   EnableConfigWatcher == other.EnableConfigWatcher &&
				   UpdateIntervalInHours == other.UpdateIntervalInHours &&
				   GpioSafeMode == other.GpioSafeMode &&
				   Equals(OutputModePins, other.OutputModePins) && Equals(InputModePins, other.InputModePins) &&
				   DisplayStartupMenu == other.DisplayStartupMenu && EnableGpioControl == other.EnableGpioControl &&
				   Debug == other.Debug && EnableFirstChanceLog == other.EnableFirstChanceLog &&
				   EnableTextToSpeech == other.EnableTextToSpeech && MuteAssistant == other.MuteAssistant &&
				   CloseRelayOnShutdown == other.CloseRelayOnShutdown && ServerAuthCode == other.ServerAuthCode &&
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
