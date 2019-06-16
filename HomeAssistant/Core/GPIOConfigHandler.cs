using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Core {

	public class GPIOConfigRoot : IEquatable<GPIOConfigRoot> {

		public bool Equals (GPIOConfigRoot other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return Equals(GPIOData, other.GPIOData);
		}

		public override bool Equals (object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj.GetType() != GetType()) {
				return false;
			}

			return Equals((GPIOConfigRoot) obj);
		}

		public override int GetHashCode () => (GPIOData != null ? GPIOData.GetHashCode() : 0);

		public static bool operator == (GPIOConfigRoot left, GPIOConfigRoot right) => Equals(left, right);

		public static bool operator != (GPIOConfigRoot left, GPIOConfigRoot right) => !Equals(left, right);

		[JsonProperty]
		public List<GPIOPinConfig> GPIOData { get; set; }
	}

	public class GPIOPinConfig {

		[JsonProperty]
		public int Pin { get; set; } = 0;

		[JsonProperty]
		public bool IsOn { get; set; } = false;

		[JsonProperty]
		public PinMode Mode { get; set; } = PinMode.Output;
	}

	public class GPIOConfigHandler {
		private readonly Logger Logger = new Logger("GPIO-CONFIG-HANDLER");

		private GPIOConfigRoot RootObject;

		public GPIOConfigRoot SaveGPIOConfig(GPIOConfigRoot Config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(Config, Formatting.Indented);
			string pathName = Constants.GPIOConfigPath;
			using (StreamWriter sw = new StreamWriter(pathName, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, Config);
					Logger.Log("Updated GPIO Config!");
					sw.Dispose();
					return Config;
				}
			}
		}

		public GPIOConfigRoot LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Such a folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.GPIOConfigPath)) {
				bool loaded = GenerateDefaultConfig();
				if (!loaded) {
					return null;
				}
			}

			string JSON;
			using (FileStream Stream = new FileStream(Constants.GPIOConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			RootObject = JsonConvert.DeserializeObject<GPIOConfigRoot>(JSON);
			Logger.Log("GPIO Configuration Loaded Successfully!");
			return RootObject;
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
						System.Threading.Tasks.Task.Run(async () => await Tess.Exit().ConfigureAwait(false));
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

			if (File.Exists(Constants.GPIOConfigPath)) {
				return true;
			}

			GPIOConfigRoot Config = new GPIOConfigRoot {
				GPIOData = new List<GPIOPinConfig>()
			};

			for (int i = 0; i <= 31; i++) {
				GPIOPinConfig PinConfig = new GPIOPinConfig() {
					IsOn = false,
					Mode = PinMode.Output,
					Pin = i
				};

				Config.GPIOData.Add(PinConfig);
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(Config, Formatting.Indented);
			string pathName = Constants.GPIOConfigPath;
			using (StreamWriter sw = new StreamWriter(pathName, false))
			using (JsonWriter writer = new JsonTextWriter(sw)) {
				writer.Formatting = Formatting.Indented;
				serializer.Serialize(writer, Config);
				sw.Dispose();
			}
			return true;
		}
	}
}
