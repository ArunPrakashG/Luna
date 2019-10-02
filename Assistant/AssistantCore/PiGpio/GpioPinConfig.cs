using Assistant.Extensions;
using Assistant.Log;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio {
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
		private static readonly Logger Logger = new Logger("GPIO-CONFIG");

		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}

			GpioPinConfig config = obj as GpioPinConfig;
			return config.Pin == Pin;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override string ToString() {
			StringBuilder s = new StringBuilder();
			s.AppendLine("---------------------------");
			s.AppendLine($"Pin -> {Pin}");
			s.AppendLine($"Pin Value -> {PinValue.ToString()}");
			s.AppendLine($"Pin Mode -> {Mode.ToString()}");
			s.AppendLine($"Is Tasked -> {IsDelayedTaskSet}");
			s.AppendLine($"Task set after minutes -> {TaskSetAfterMinutes}");
			s.AppendLine($"Is pin on -> {IsPinOn}");
			s.AppendLine("---------------------------");
			return s.ToString();
		}

		public GpioPinConfig(int _pin, GpioPinState _pinValue, GpioPinMode _mode, bool _isDelayedTaskSet, int _taskSetAfterMinutes) {
			Pin = _pin;
			PinValue = _pinValue;
			Mode = _mode;
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

			if (Helpers.IsNullOrEmpty(json)) {
				return;
			}

			string fileName = config.Pin + ".json";
			string filePath = Constants.GpioConfigDirectory + "/" + fileName;
			string newFilePath = filePath + ".new";

			if (Core.ConfigWatcher.FileSystemWatcher != null) {
				Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = false;
			}

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

				if (Core.ConfigWatcher.FileSystemWatcher != null) {
					Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = true;
				}
			}
		}
	}
}
