using Assistant.Extensions;
using Assistant.Log;
using Assistant.Modules.Interfaces;
using Newtonsoft.Json;
using System.IO;
using Assistant.AssistantCore;

namespace Discord {

	public class DiscordBotConfig : IDiscordBotConfig {

		[JsonProperty] public bool EnableDiscordBot { get; set; }

		[JsonProperty] public ulong DiscordOwnerID { get; set; } = 161859532920848384;

		[JsonProperty] public ulong DiscordServerID { get; set; } = 580995322369802240;

		[JsonProperty] public ulong DiscordLogChannelID { get; set; } = 580995512526831616;

		[JsonProperty] public bool DiscordLog { get; set; } = true;

		[JsonProperty] public string DiscordBotToken { get; set; }

		private static Logger Logger = new Logger("DISCORD-CONFIG");

		[JsonIgnore]
		public const string DiscordBotConfigPath = Constants.ConfigDirectory + "/DiscordBot.json";

		public static DiscordBotConfig LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...", Enums.LogLevels.Trace);
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			string JSON;
			using (FileStream Stream = new FileStream(DiscordBotConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			DiscordBotConfig returnConfig = JsonConvert.DeserializeObject<DiscordBotConfig>(JSON);
			Logger.Log("Discord Configuration Loaded Successfully!", Enums.LogLevels.Trace);
			return returnConfig;
		}

		public static DiscordBotConfig SaveConfig(IDiscordBotConfig config) {
			config = (DiscordBotConfig) config;
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...", Enums.LogLevels.Trace);
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(DiscordBotConfigPath)) {
				Logger.Log("Discord config file doesn't exist.", Enums.LogLevels.Warn);
				return null;
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(config, Formatting.Indented);
			using (StreamWriter sw = new StreamWriter(DiscordBotConfigPath, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, config);
					Logger.Log("Updated Discord BotConfig!", Enums.LogLevels.Trace);
					sw.Dispose();
					return config as DiscordBotConfig;
				}
			}
		}
	}
}
