using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;

namespace Assistant.Gpio {
	public class GpioConfigHandler {
		private readonly ILogger Logger = new Logger(typeof(GpioConfigHandler).Name);

		[JsonProperty]
		public List<GpioPinConfig> PinConfigs { get; private set; } = new List<GpioPinConfig>();

		private static readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);
		public static bool IsConfigBeingAccessed { get; private set; } = false;

		public void Save(List<GpioPinConfig> configs) {
			if (configs == null || configs.Count <= 0) {
				return;
			}

			configs.ForEach(async (x) => await SaveAsync(x).ConfigureAwait(false));
		}

		public async Task SaveAsync(GpioPinConfig config) {
			if(config == null) {
				return;
			}

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			string filePath = Constants.GpioConfigDirectory;
			string json = JsonConvert.SerializeObject(config, Formatting.Indented);

			if (string.IsNullOrEmpty(json)) {
				return;
			}

			string newFilePath = filePath + ".new";

			await ConfigSemaphore.WaitAsync().ConfigureAwait(false);
			IsConfigBeingAccessed = true;

			try {
				File.WriteAllText(newFilePath, json);

				if (File.Exists(filePath)) {
					File.Replace(newFilePath, filePath, null);
				}
				else {
					File.Move(newFilePath, filePath);
				}

				Logger.Log("Saved config!", LogLevels.Trace);
			}
			catch (Exception e) {
				Logger.Log(e);
				Logger.Log("Failed to save config.", LogLevels.Trace);
				return;
			}
			finally {
				ConfigSemaphore.Release();
				IsConfigBeingAccessed = false;
			}
		}

		public async Task<GpioConfigHandler?> LoadAsync() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Such a folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.GpioConfigDirectory) && !await GenerateDefaultConfig().ConfigureAwait(false)) {
				return null;
			}

			Logger.Log("Loading Gpio config...", LogLevels.Trace);

			await ConfigSemaphore.WaitAsync().ConfigureAwait(false);
			IsConfigBeingAccessed = true;
			try {
				using FileStream Stream = new FileStream(Constants.GpioConfigDirectory, FileMode.Open, FileAccess.Read);
				using StreamReader ReadSettings = new StreamReader(Stream);
				GpioConfigHandler configRoot = JsonConvert.DeserializeObject<GpioConfigHandler>(ReadSettings.ReadToEnd());
				PinConfigs = configRoot.PinConfigs;
				return this;
			}
			catch (Exception e) {
				Logger.Log(e);
				Logger.Trace("Failed to load config.");
				return null;
			}
			finally {
				ConfigSemaphore.Release();
				IsConfigBeingAccessed = false;
			}
		}

		public async Task<bool> GenerateDefaultConfig() {
			Logger.Log("Gpio config file doesn't exist. press c to continue generating default config or q to quit...");
			await Task.Delay(500).ConfigureAwait(false);

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			ConsoleKeyInfo? Key = Helpers.FetchUserInputSingleChar(TimeSpan.FromMinutes(1));

			if (!Key.HasValue) {
				Logger.Log("No value has been entered, continuing to run the program...");
			}
			else {
				switch (Key.Value.KeyChar) {
					case 'c':
						break;

					case 'q':
						Environment.Exit(-1);
						return false;

					default:
						Logger.Log("Unknown value entered! continuing to run the program...");
						break;
				}
			}

			Logger.Log("Generating default Gpio Config...");

			if (File.Exists(Constants.GpioConfigDirectory)) {
				return true;
			}

			GpioConfigHandler Config = new GpioConfigHandler {
				PinConfigs = new List<GpioPinConfig>()
			};

			for (int i = 0; i <= 41; i++) {
				GpioPinConfig PinConfig = new GpioPinConfig(i, PiController.GpioPinState.Off, PiController.GpioPinMode.Output, false, 0);
				Config.PinConfigs.Add(PinConfig);
			}

			Save(Config.PinConfigs);
			return true;
		}
	}
}
