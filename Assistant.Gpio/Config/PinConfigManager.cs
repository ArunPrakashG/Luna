using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Gpio.Config.PinConfig;
using static Assistant.Gpio.Controllers.PiController;

namespace Assistant.Gpio.Config {
	/// <summary>
	/// Defines the <see cref="PinConfigManager" />
	/// </summary>
	public class PinConfigManager {
		private readonly ILogger Logger = new Logger(typeof(PinConfigManager).Name);
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static PinConfig PinConfig;

		/// <summary>
		/// Saves the pin configuration.
		/// </summary>
		/// <param name="config">The config to save <see cref="Pin"/></param>
		/// <returns>The <see cref="Task"/></returns>
		public async Task SaveConfig(Pin config) {
			if (config == null) {
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				string json = JsonConvert.SerializeObject(config, Formatting.Indented);

				if (string.IsNullOrEmpty(json)) {
					return;
				}

				string fileName = config.PinNumber + ".json";
				string filePath = Constants.GpioConfigDirectory + "/" + fileName;
				string newFilePath = filePath + ".new";

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
				return;
			}
			finally {
				Sync.Release();
			}
		}

		/// <summary>
		/// Saves the pin configuration as a whole.
		/// </summary>
		/// <param name="pinConfigCollection">The pin config collection <see cref="PinConfig"/></param>
		/// <returns>The <see cref="Task"/></returns>
		public async Task SaveConfig(PinConfig pinConfigCollection) {
			if (pinConfigCollection == null || pinConfigCollection.PinConfigs.Count <= 0) {
				return;
			}

			foreach (Pin pin in pinConfigCollection.PinConfigs) {
				if (pin == null) {
					continue;
				}

				await SaveConfig(pin).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Saves the pin config collection.
		/// </summary>
		/// <param name="pinConfigs">The pin configs <see cref="List{Pin}"/></param>
		/// <returns>The <see cref="Task"/></returns>
		public async Task SaveConfig(List<Pin> pinConfigs) {
			if (pinConfigs == null || pinConfigs.Count <= 0) {
				return;
			}

			foreach (Pin pin in pinConfigs) {
				if (pin == null) {
					continue;
				}

				await SaveConfig(pin).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Gets the configuration.
		/// </summary>
		/// <returns>The <see cref="PinConfig?"/></returns>
		public static PinConfig GetConfiguration() => PinConfig;

		/// <summary>
		/// Loads the pin configuration.
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		public async Task LoadConfiguration() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.GpioConfigDirectory) && !GenerateDefault()) {
				return;
			}

			Logger.Trace("Loading Gpio config...");
			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				using FileStream stream = new FileStream(Constants.GpioConfigDirectory, FileMode.Open, FileAccess.Read);
				using StreamReader reader = new StreamReader(stream);
				PinConfig = JsonConvert.DeserializeObject<PinConfig>(reader.ReadToEnd());

				if (PinConfig != null && PinConfig.PinConfigs.Count > 0)
					Logger.Trace("Pin configuration loaded!");
				else
					Logger.Warning("Failed to load pin configuration.");
			}
			catch (Exception e) {
				Logger.Log(e);
			}
			finally {
				Sync.Release();
			}
		}

		/// <summary>
		/// Generates a default configuration.
		/// </summary>
		/// <returns>The <see cref="bool"/> status of the generation</returns>
		private bool GenerateDefault() {
			if (File.Exists(Constants.GpioConfigDirectory)) {
				return false;
			}

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			Logger.Log("Generating default Gpio Config...");
			List<Pin> pinConfigs = new List<Pin>();

			foreach (int pin in Constants.BcmGpioPins) {
				Pin pinConfig = new Pin(pin, GpioPinState.Off, GpioPinMode.Input);
				pinConfigs.Add(pinConfig);
			}

			return true;
		}
	}
}
