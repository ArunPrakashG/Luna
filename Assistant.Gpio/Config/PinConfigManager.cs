using Assistant.Extensions;
using Assistant.Gpio.Controllers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Gpio.Config {
	/// <summary>
	/// Defines the <see cref="PinConfigManager" />
	/// </summary>
	public class PinConfigManager {
		private readonly ILogger Logger = new Logger(typeof(PinConfigManager).Name);
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private readonly GpioController Controller;
		private static PinConfig PinConfig;

		internal PinConfigManager(GpioController _controller) => Controller = _controller;

		internal void Init(PinConfig _config) => PinConfig = _config;

		/// <summary>
		/// Saves the pin configuration as a whole.
		/// </summary>
		/// <param name="pinConfig">The pin config collection <see cref="PinConfig"/></param>
		/// <returns>The <see cref="Task"/></returns>
		internal async Task SaveConfig() {
			if (PinConfig == null || PinConfig.PinConfigs.Count <= 0) {
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				string json = JsonConvert.SerializeObject(PinConfig, Formatting.Indented);

				if (string.IsNullOrEmpty(json)) {
					return;
				}

				await File.WriteAllTextAsync(Constants.GpioConfigDirectory, json).ConfigureAwait(false);
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
		/// Gets the configuration.
		/// </summary>
		/// <returns>The <see cref="PinConfig?"/></returns>
		internal static PinConfig GetConfiguration() => PinConfig;

		/// <summary>
		/// Loads the pin configuration.
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		internal async Task<bool> LoadConfiguration() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.GpioConfigDirectory)) {
				return false;
			}

			Logger.Trace("Loading Gpio config...");
			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				PinConfig = JsonConvert.DeserializeObject<PinConfig>(File.ReadAllText(Constants.GpioConfigDirectory));

				if (PinConfig != null && PinConfig.PinConfigs.Count > 0) {
					Logger.Trace("Pin configuration loaded!");
					return true;
				}

				Logger.Warning("Failed to load pin configuration.");
				return false;
			}
			catch (Exception e) {
				Logger.Log(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}
	}
}
