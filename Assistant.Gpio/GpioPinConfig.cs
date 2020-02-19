using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using static Assistant.Gpio.PiController;

namespace Assistant.Gpio {
	public class GpioPinConfig {
		[JsonProperty]
		public int Pin { get; set; } = 0;

		[JsonProperty]
		public GpioPinState PinValue { get; set; }

		[JsonProperty]
		public GpioPinMode Mode { get; set; }

		[JsonProperty]
		public bool IsDelayedTaskSet { get; set; }

		[JsonProperty]
		public int TaskSetAfterMinutes { get; set; }

		[JsonProperty]
		public bool IsPinOn => PinValue == GpioPinState.On;

		private static readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);
		private static readonly ILogger Logger = new Logger(typeof(GpioPinConfig).Name);

		public override bool Equals(object? obj) {
			if (obj == null) {
				return false;
			}

			GpioPinConfig config = (GpioPinConfig) obj;
			return config.Pin == this.Pin;
		}

		public override int GetHashCode() => base.GetHashCode();

		public static string AsJson(GpioPinConfig? config) {
			if (config == null) {
				return string.Empty;
			}

			return JsonConvert.SerializeObject(config);
		}

		public override string ToString() {
			StringBuilder s = new StringBuilder();
			s.AppendLine("---------------------------");
			s.AppendLine($"Pin -> {this.Pin}");
			s.AppendLine($"Pin Value -> {PinValue.ToString()}");
			s.AppendLine($"Pin Mode -> {this.Mode.ToString()}");
			s.AppendLine($"Is Tasked -> {IsDelayedTaskSet}");
			s.AppendLine($"Task set after minutes -> {TaskSetAfterMinutes}");
			s.AppendLine($"Is pin on -> {IsPinOn}");
			s.AppendLine("---------------------------");
			return s.ToString();
		}

		public GpioPinConfig() {

		}

		public GpioPinConfig(int _pin, GpioPinState _pinValue, GpioPinMode _mode, bool _isDelayedTaskSet, int _taskSetAfterMinutes) {
			this.Pin = _pin;
			PinValue = _pinValue;
			this.Mode = _mode;
			IsDelayedTaskSet = _isDelayedTaskSet;
			TaskSetAfterMinutes = _taskSetAfterMinutes;
		}

		private static void SaveConfig(GpioPinConfig config) {
			if (config == null) {
				return;
			}

			ConfigSemaphore.Wait();

			if (!Directory.Exists(Constants.GpioConfigDirectory)) {
				Directory.CreateDirectory(Constants.GpioConfigDirectory);
			}

			string json = JsonConvert.SerializeObject(config, Formatting.Indented);

			if (string.IsNullOrEmpty(json)) {
				return;
			}

			string fileName = config.Pin + ".json";
			string filePath = Constants.GpioConfigDirectory + "/" + fileName;
			string newFilePath = filePath + ".new";

			try {
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
			}
			finally {
				ConfigSemaphore.Release();
			}
		}
	}
}
