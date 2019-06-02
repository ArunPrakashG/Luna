using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Core {

	public class GPIOConfigRoot {
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
		private Logger Logger = new Logger("GPIO-CONFIG-HANDLER");

		private GPIOConfigRoot RootObject;

		public void SaveGPIOConfig(GPIOConfigRoot Config) {
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

			string JSON = null;
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

			Task waitTask = Task.Delay(60000);

			Task<ConsoleKeyInfo> inputTask = Task.Run(() => {
				return Console.ReadKey();
			});

			Task.WhenAny(new[] { waitTask, inputTask });

			switch (inputTask.Result.KeyChar) {
				case 'c':
					break;
				case 'q':
					Task.Run(async () => await Program.Exit(0).ConfigureAwait(false));
					return false;
				default:
					Logger.Log("Unknown value entered! continuing to run the program...");
					goto case 'c';
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
