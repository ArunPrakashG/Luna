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
	[Serializable]
	public class CoreConfig {
		[JsonProperty]
		public bool AutoUpdates { get; set; } = true;

		[JsonProperty]
		public bool EnableModules { get; set; } = true;

		[JsonProperty]
		public bool GpioSafeMode { get; set; } = false;

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

		[JsonProperty]
		public bool Debug { get; set; } = false;

		[JsonProperty]
		public string? StatisticsServerIP { get; set; }

		[JsonProperty]
		public string? OpenWeatherApiKey { get; set; }

		[JsonProperty]
		public string? PushBulletApiKey { get; set; }

		[JsonProperty]
		public string AssistantDisplayName { get; set; } = "Home Assistant";

		[JsonProperty]
		public DateTime ProgramLastStartup { get; set; }

		[JsonProperty]
		public DateTime ProgramLastShutdown { get; set; }

		private static readonly ILogger Logger = new Logger(typeof(CoreConfig).Name);
		private static readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);

		public static async Task<CoreConfig?> SaveConfig(CoreConfig config) {
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

			return this == (CoreConfig) obj;
		}

		public override string? ToString() => base.ToString();

		public static bool operator ==(CoreConfig left, CoreConfig right) => Equals(left, right);

		public static bool operator !=(CoreConfig left, CoreConfig right) => !Equals(left, right);
	}
}
