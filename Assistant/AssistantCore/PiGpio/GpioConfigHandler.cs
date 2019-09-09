
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.Extensions;
using Assistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Assistant.AssistantCore.PiGpio {

	public class GpioConfigRoot : IEquatable<GpioConfigRoot> {

		public bool Equals(GpioConfigRoot other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return Equals(GPIOData, other.GPIOData);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj.GetType() != GetType()) {
				return false;
			}

			return Equals((GpioConfigRoot) obj);
		}

		public override int GetHashCode() => GPIOData != null ? GPIOData.GetHashCode() : 0;

		public static bool operator ==(GpioConfigRoot left, GpioConfigRoot right) => Equals(left, right);

		public static bool operator !=(GpioConfigRoot left, GpioConfigRoot right) => !Equals(left, right);

		[JsonProperty]
		public List<GpioPinConfig> GPIOData { get; set; }
	}

	public class GpioPinConfig {

		[JsonProperty]
		public int Pin { get; set; } = 0;

		[JsonProperty]
		public bool IsOn { get; set; } = false;

		[JsonProperty]
		public Enums.PinMode Mode { get; set; } = Enums.PinMode.Output;
	}

	public class GpioConfigHandler {
		private readonly Logger Logger = new Logger("GPIO-CONFIG-HANDLER");

		private GpioConfigRoot RootObject;
		private static readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);

		public GpioConfigRoot SaveGPIOConfig(GpioConfigRoot config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			string filePath = Constants.GpioConfigPath;
			string json = JsonConvert.SerializeObject(config, Formatting.Indented);
			string newFilePath = filePath + ".new";

			ConfigSemaphore.Wait();
			Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = false;

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
				Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = true;
				return config;
			}
			finally {
				ConfigSemaphore.Release();
			}

			Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = true;
			Logger.Log("Saved config!", Enums.LogLevels.Trace);
			return config;
		}

		public GpioConfigRoot LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Such a folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.GpioConfigPath) && !GenerateDefaultConfig()) {
				return null;
			}

			string JSON;
			Logger.Log("Loading Gpio config...", Enums.LogLevels.Trace);
			ConfigSemaphore.Wait();
			using (FileStream Stream = new FileStream(Constants.GpioConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			GpioConfigRoot config = JsonConvert.DeserializeObject<GpioConfigRoot>(JSON);
			ConfigSemaphore.Release();
			Logger.Log("Gpio configuration loaded successfully!", Enums.LogLevels.Trace);
			return config;
		}

		public bool GenerateDefaultConfig() {
			Logger.Log("GPIO config file doesnt exist. press c to continue generating default config or q to quit.");

			ConsoleKeyInfo? Key = Helpers.FetchUserInputSingleChar(TimeSpan.FromMinutes(1));

			if (!Key.HasValue) {
				Logger.Log("No value has been entered, continuing to run the program...");
			}
			else {
				switch (Key.Value.KeyChar) {
					case 'c':
						break;

					case 'q':
						System.Threading.Tasks.Task.Run(async () => await Core.Exit().ConfigureAwait(false));
						return false;

					default:
						Logger.Log("Unknown value entered! continuing to run the program...");
						break;
				}
			}

			Logger.Log("Generating default GPIO Config...");

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesnt exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (File.Exists(Constants.GpioConfigPath)) {
				return true;
			}

			GpioConfigRoot Config = new GpioConfigRoot {
				GPIOData = new List<GpioPinConfig>()
			};

			for (int i = 0; i <= 31; i++) {
				GpioPinConfig PinConfig = new GpioPinConfig() {
					IsOn = false,
					Mode = Enums.PinMode.Output,
					Pin = i
				};

				Config.GPIOData.Add(PinConfig);
			}

			SaveGPIOConfig(Config);
			return true;
		}
	}
}
