using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna {
	public class ApiKeys {
		[JsonProperty]
		public string? OpenWeatherApiKey { get; set; }

		[JsonProperty]
		public string? PushBulletApiKey { get; set; }
	}

	public class GpioConfiguration {
		[JsonProperty]
		public bool GpioSafeMode { get; set; }

		[JsonProperty]
		public int[] OutputModePins = new int[] { 2, 3, 4, 17, 27, 22, 10, 9 };

		[JsonProperty]
		public int[] InputModePins = new int[] { 26, 20, 16 };

		[JsonProperty]
		public int[] InfraredSensorPins = new int[] { 26, 20 };

		[JsonProperty]
		public int[] SoundSensorPins = new int[] { 16 };

		[JsonProperty]
		public int[] RelayPins = new int[] { 2, 3, 4, 17, 27, 22, 10, 9 };

		[JsonProperty]
		public GpioDriver GpioDriverProvider { get; set; } = GpioDriver.RaspberryIODriver;

		[JsonProperty]
		public NumberingScheme PinNumberingScheme { get; set; } = NumberingScheme.Logical;
	}

	public class CoreConfig {
		[JsonProperty]
		public string PublicIP { get; set; }

		[JsonProperty]
		public string LocalIP { get; set; }

		[JsonProperty]
		public bool AutoUpdates { get; set; }

		[JsonProperty]
		public bool EnableModules { get; set; }

		[JsonProperty]
		public bool EnableShell { get; set; }

		[JsonProperty]
		public GpioConfiguration GpioConfiguration { get; set; } = new GpioConfiguration();

		[JsonProperty]
		public bool Debug { get; set; } = false;

		[JsonProperty]
		public int RestServerPort { get; set; } = 7577;

		[JsonProperty]
		public string? StatisticsServerIP { get; set; }

		[JsonProperty]
		public ApiKeys ApiKeys { get; set; } = new ApiKeys();

		private readonly ILogger Logger = new Logger(typeof(CoreConfig).Name);
		private readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);
		private readonly Core Core;

		[JsonConstructor]
		internal CoreConfig() { }

		internal CoreConfig(Core _core) => Core = _core ?? throw new ArgumentNullException(nameof(_core));

		internal async Task SaveAsync() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			await ConfigSemaphore.WaitAsync().ConfigureAwait(false);
			Core.GetFileWatcher().Pause();

			Logger.Log("Saving core config...", LogLevels.Trace);

			try {
				string filePath = Constants.CoreConfigPath;
				string json = JsonConvert.SerializeObject(this, Formatting.Indented);
				string newFilePath = filePath + ".new";

				using (StreamWriter writer = new StreamWriter(newFilePath)) {
					writer.Write(json);
					writer.Flush();
				}

				if (File.Exists(filePath)) {
					File.Replace(newFilePath, filePath, null);
				}
				else {
					File.Move(newFilePath, filePath);
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return;
			}
			finally {
				ConfigSemaphore.Release();
				Core.GetFileWatcher().Resume();
			}

			Logger.Log("Saved core config!", LogLevels.Trace);
		}

		internal async Task LoadAsync() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.CoreConfigPath) && !GenerateDefaultConfig()) {
				return;
			}

			await ConfigSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				Logger.Log("Loading core config...", LogLevels.Trace);
				using (StreamReader reader = new StreamReader(new FileStream(Constants.CoreConfigPath, FileMode.Open, FileAccess.Read))) {
					string jsonContent = reader.ReadToEnd();

					if (string.IsNullOrEmpty(jsonContent)) {
						return;
					}

					CoreConfig config = JsonConvert.DeserializeObject<CoreConfig>(jsonContent);
					this.AutoUpdates = config.AutoUpdates;
					this.Debug = config.Debug;
					this.EnableModules = config.EnableModules;
					this.GpioConfiguration = config.GpioConfiguration;
					this.ApiKeys = config.ApiKeys;
					this.StatisticsServerIP = config.StatisticsServerIP;
				}

				Logger.Log("Core configuration loaded successfully!", LogLevels.Trace);
			}
			catch (Exception e) {
				Logger.Log(e);
				return;
			}
			finally {
				ConfigSemaphore.Release();
			}
		}

		internal bool GenerateDefaultConfig() {
			Logger.Log("Generating default config...");
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (File.Exists(Constants.CoreConfigPath)) {
				return true;
			}

			SaveAsync().RunSynchronously();
			return true;
		}

		public override string? ToString() => JsonConvert.SerializeObject(this);

		public override int GetHashCode() => base.GetHashCode();

		public override bool Equals(object? obj) {
			if (obj == null) {
				return false;
			}

			CoreConfig? config = obj as CoreConfig;

			if (config == null) {
				return false;
			}

			return config.GetHashCode() == this.GetHashCode();
		}
	}
}
