using Luna.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Gpio {
	/// <summary>
	/// Defines pin configuration collection.
	/// </summary>
	internal class PinConfig {
		[JsonProperty]
		internal bool SafeMode { get; set; }

		/// <summary>
		/// Defines the PinConfigs
		/// </summary>
		[JsonProperty]
		internal List<Pin> PinConfigs { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PinConfig"/> class.
		/// </summary>
		/// <param name="configs">The configs<see cref="List{Pin}"/></param>
		internal PinConfig(List<Pin> configs, bool safeMode) {
			PinConfigs = configs;
			SafeMode = safeMode;
		}

		[JsonConstructor]
		internal PinConfig() { }

		internal Pin this[int index] => PinConfigs[index];

		private readonly InternalLogger Logger = new InternalLogger(nameof(PinConfig));
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);

		private static void ClearFile(string filePath) {
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
				return;
			}

			using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write)) {
				lock (fileStream) {
					fileStream.SetLength(0);
				}

				fileStream.Close();
			}
		}

		/// <summary>
		/// Saves the pin configuration as a whole.
		/// </summary>
		/// <param name="pinConfig">The pin config collection <see cref="PinConfig"/></param>
		/// <returns>The <see cref="Task"/></returns>
		internal async Task SaveConfig() {
			if (PinConfigs == null || PinConfigs.Count <= 0) {
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				string json = JsonConvert.SerializeObject(this, Formatting.Indented);

				ClearFile(Constants.GpioConfigPath);
				using (FileStream fileStream = new FileStream(Constants.GpioConfigPath, FileMode.OpenOrCreate, FileAccess.Write)) {
					using (StreamWriter writer = new StreamWriter(fileStream)) {
						await writer.WriteAsync(json).ConfigureAwait(false);
						await writer.FlushAsync().ConfigureAwait(false);
					}
				}
			}
			catch (Exception e) {
				Logger.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		/// <summary>
		/// Loads the pin configuration.
		/// </summary>
		/// <returns>The <see cref="Task"/></returns>
		internal async Task<bool> LoadConfiguration() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.GpioConfigPath)) {
				return false;
			}

			Logger.Trace("Loading Gpio config...");
			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				var config = JsonConvert.DeserializeObject<PinConfig>(File.ReadAllText(Constants.GpioConfigPath));

				if (config != null && config.PinConfigs.Count > 0) {
					this.PinConfigs = config.PinConfigs;
					this.SafeMode = config.SafeMode;
					Logger.Trace("Pin configuration loaded!");
					return true;
				}

				Logger.Warn("Failed to load pin configuration.");
				return false;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}
	}

	/// <summary>
	/// Defines the pin configuration of the pin it holds.
	/// </summary>	
	internal struct Pin {
		/// <summary>
		/// The pin.
		/// </summary>		
		internal readonly int PinNumber;

		/// <summary>
		/// Gets or sets the Pin state. (On/Off)
		/// </summary>		
		internal GpioPinState PinState;

		/// <summary>
		/// Gets or sets the Pin mode. (Output/Input)
		/// </summary>		
		internal GpioPinMode Mode;

		/// <summary>
		/// Gets or sets a value indicating whether the pin is available.
		/// </summary>		
		internal bool IsAvailable;

		/// <summary>
		/// Gets or sets the Scheduler job name if the pin isn't available.
		/// </summary>		
		internal string? JobName { get; set; }

		/// <summary>
		/// Gets a value indicating whether IsPinOn
		/// Gets a value indicating the pin current state. <see cref="PinState"/>
		/// </summary>		
		internal bool IsPinOn => PinState == GpioPinState.On;

		/// <summary>
		/// Initializes a new instance of the <see cref="Pin"/> class.
		/// </summary>
		/// <param name="pin">The pin <see cref="int"/></param>
		/// <param name="state">The state <see cref="GpioPinState"/></param>
		/// <param name="mode">The mode <see cref="GpioPinMode"/></param>
		/// <param name="available">The status if the pin is currently available <see cref="bool"/></param>
		/// <param name="jobName">The jobName of the scheduler if the pin isn't available at the moment.<see cref="string?"/></param>
		internal Pin(int pin, GpioPinState state, GpioPinMode mode, bool available = true, string? jobName = null) {
			PinNumber = pin;
			PinState = state;
			Mode = mode;
			IsAvailable = available;
			JobName = jobName;
		}

		internal Pin(int pin, GpioPinMode mode, bool available = true, string? jobName = null) {
			PinNumber = pin;
			PinState = GpioPinState.Off;
			Mode = mode;
			IsAvailable = available;
			JobName = jobName;
		}

		internal Pin(int pin, GpioPinState state, bool available = true, string? jobName = null) {
			PinNumber = pin;
			PinState = state;
			Mode = GpioPinMode.Input;
			IsAvailable = available;
			JobName = jobName;
		}

		/// <summary>
		/// Gets a summary of the pin configuration this object holds.
		/// </summary>
		/// <returns>The <see cref="string"/></returns>
		public override string ToString() {
			StringBuilder s = new StringBuilder();
			s.AppendLine("---------------------------");
			s.AppendLine($"Pin -> {PinNumber}");
			s.AppendLine($"Pin Value -> {PinState}");
			s.AppendLine($"Pin Mode -> {Mode}");
			s.AppendLine($"Is Tasked -> {!IsAvailable}");
			s.AppendLine($"Task Name -> {JobName}");
			s.AppendLine($"Is pin on -> {IsPinOn}");
			s.AppendLine("---------------------------");
			return s.ToString();
		}

		/// <summary>
		/// Compares both objects.
		/// </summary>
		/// <param name="obj">The obj<see cref="object?"/></param>
		/// <returns>The <see cref="bool"/></returns>
		public override bool Equals(object? obj) {
			if (obj == null) {
				return false;
			}

			Pin config = (Pin) obj;
			return config.PinNumber == PinNumber;
		}

		/// <summary>
		/// Gets the hash code of the object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => base.GetHashCode();
	}
}
