using Assistant.Modules.Interfaces;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using Newtonsoft.Json;
using SteamKit2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static HomeAssistant.AssistantCore.Enums;

namespace Steam {

	public class Steam : IModuleBase, ISteamClient {
		private readonly Logger Logger = new Logger("STEAM");

		public bool RequiresInternetConnection { get; set; } = true;
		public uint SteamCellID { get; set; }
		public ISteamConfig SteamConfig { get; set; }

		public ConcurrentDictionary<string, ISteamBot> SteamBotCollection { get; set; } = new ConcurrentDictionary<string, ISteamBot>();

		public string BotConfigDirectory { get; set; } = Constants.ConfigDirectory + "/SteamBots/";
		public string SteamConfigPath { get; set; } = Constants.ConfigDirectory + "/SteamConfig.json";

		public long ModuleIdentifier { get; set; }

		public Version ModuleVersion { get; } = new Version("3.0.0.0");

		public string ModuleAuthor { get; } = "Arun Prakash";

		public (bool, ConcurrentDictionary<string, ISteamBot>) InitSteamBots() {
			if (!Directory.Exists(BotConfigDirectory)) {
				Logger.Log("BotConfig directory doesn't exist!", LogLevels.Warn);
				return (false, new ConcurrentDictionary<string, ISteamBot>());
			}

			SteamBotCollection.Clear();

			try {
				List<ISteamBotConfig> botConfigs = LoadSteamBotConfig();

				Logger.Log($"{botConfigs.Count} accounts found, starting creation of bot instances...",
					LogLevels.Trace);

				foreach (ISteamBotConfig config in botConfigs) {
					if (config == null) {
						continue;
					}

					string botName = config.SteamID ?? throw new ArgumentNullException("botName is null");
					_ = config.SteamID ??
						throw new ArgumentNullException("Steam bot steam ID is empty or null", config.SteamID);
					_ = config.SteamPass ??
						throw new ArgumentNullException("Steam bot password is empty or null", config.SteamPass);

					int connectionTry = 1;
					int maxConnectionRetry = 5;

					while (true) {
						if (connectionTry > maxConnectionRetry) {
							Logger.Log(
								$"Connection to steam for {config.SteamID} failed after {maxConnectionRetry} times.",
								LogLevels.Error);
							return (false, null);
						}

						try {
							SteamConfiguration steamConfig = SteamConfiguration.Create(builder => builder.WithProtocolTypes(ProtocolTypes.All));
							SteamClient botClient = new SteamClient(steamConfig);
							CallbackManager manager = new CallbackManager(botClient);
							Logger BotLogger = new Logger(botName);
							Bot Bot = new Bot();

							(bool initStatus, ISteamBot botInstance) steamBotStatus = Task.Run(async () =>
								await Bot.RegisterSteamBot(botName, BotLogger, botClient, this, manager, config).ConfigureAwait(false)).Result;

							if (steamBotStatus.initStatus && steamBotStatus.botInstance != null) {
								Logger.Log($"{botName} account connected and authenticated successfully!");
								AddSteamBotToCollection(botName, Bot);
								continue;
							}
							else {
								Logger.Log(
									$"Failed to load {botName}, we will try again! ({connectionTry++}/{maxConnectionRetry})");
								continue;
							}
						}
						catch (ArgumentNullException an) {
							connectionTry++;
							Logger.Log(an);
						}
						catch (Exception e) {
							connectionTry++;
							Logger.Log(e);
						}
					}
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return (false, SteamBotCollection);
			}

			return (true, SteamBotCollection);
		}

		public List<ISteamBotConfig> LoadSteamBotConfig() {
			if (!Directory.Exists(BotConfigDirectory)) {
				Logger.Log("Steam bot config directory doesn't exist!", LogLevels.Warn);
				return new List<ISteamBotConfig>();
			}

			List<string> configfiles = Directory.GetFiles(BotConfigDirectory).ToList();

			if (configfiles.Count <= 0 || configfiles == null) {
				Logger.Log("There are no config files present in the directory.", LogLevels.Trace);
				return new List<ISteamBotConfig>();
			}

			List<ISteamBotConfig> resultConfigFiles = new List<ISteamBotConfig>();

			int counter = 0;

			foreach (string filePath in configfiles) {
				if (!File.Exists(filePath)) {
					continue;
				}

				string JSON;
				using (FileStream Stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
					using (StreamReader ReadSettings = new StreamReader(Stream)) {
						JSON = ReadSettings.ReadToEnd();
					}
				}

				resultConfigFiles.Add(JsonConvert.DeserializeObject<BotConfig>(JSON));
				Logger.Log($"Loaded {resultConfigFiles[counter].SteamID} account.", LogLevels.Trace);
				counter++;
			}

			return resultConfigFiles;
		}

		public ISteamConfig LoadSteamConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesn't exist!", LogLevels.Warn);
				return new SteamConfig();
			}

			if (!File.Exists(SteamConfigPath)) {
				Logger.Log("Steam config file doesn't exist!", LogLevels.Warn);
				return new SteamConfig();
			}

			string JSON;
			using (FileStream Stream = new FileStream(SteamConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			SteamConfig steamConfig = JsonConvert.DeserializeObject<SteamConfig>(JSON);
			Logger.Log($"Loaded steam configuration.", LogLevels.Trace);
			return steamConfig;
		}

		public bool SaveSteamConfig(ISteamConfig updatedConfig) {
			if (updatedConfig == null) {
				return false;
			}

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesn't exist!", LogLevels.Warn);
				return false;
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(updatedConfig, Formatting.Indented);

			using (StreamWriter sw = new StreamWriter(SteamConfigPath, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, (SteamConfig) updatedConfig);
					Logger.Log($"Updated steam configuration file.");
					sw.Dispose();
					return true;
				}
			}
		}

		public bool SaveSteamBotConfig(string botName, ISteamBotConfig updatedConfig) {
			if (!Directory.Exists(BotConfigDirectory)) {
				Logger.Log("Steam bot config directory doesn't exist!", LogLevels.Warn);
				return false;
			}

			if (Helpers.IsNullOrEmpty(botName) || updatedConfig == null) {
				return false;
			}

			string pathName = BotConfigDirectory + botName + ".json";

			if (!File.Exists(pathName)) {

				//TODO save config only handle updates to the config for now therfore return false
				return false;
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(updatedConfig, Formatting.Indented);

			using (StreamWriter sw = new StreamWriter(pathName, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, updatedConfig);
					Logger.Log($"Updated {botName} config file.");
					sw.Dispose();
					return true;
				}
			}
		}

		public void AddSteamBotToCollection(string botName, ISteamBot bot) {
			if (SteamBotCollection.ContainsKey(botName)) {
				Logger.Log("Deleting duplicate entry of current instance.", LogLevels.Trace);
				SteamBotCollection.TryRemove(botName, out _);
			}

			SteamBotCollection.TryAdd(botName, bot);
			Logger.Log("Added current instance to client collection.", LogLevels.Trace);
		}

		public void RemoveSteamBotFromCollection(string botName) {
			if (SteamBotCollection.ContainsKey(botName)) {
				Logger.Log("Remove current bot instance from collection.", LogLevels.Trace);
				SteamBotCollection.TryRemove(botName, out _);
			}
		}

		public bool DisposeAllSteamBots() {
			if (SteamBotCollection.Count <= 0) {
				return false;
			}

			foreach (KeyValuePair<string, ISteamBot> bot in SteamBotCollection) {
				bot.Value.Dispose();
			}

			return true;
		}

		public string GetUserInput(SteamUserInputType userInputType) {
			if (userInputType == SteamUserInputType.Unknown) {
				return null;
			}

			string result = null;

			try {
				switch (userInputType) {
					case SteamUserInputType.DeviceID:
						Logger.Log("Please enter your mobile authenticator device ID (including 'android:'): ");
						result = Console.ReadLine();
						break;

					case SteamUserInputType.Login:
						Logger.Log("Please enter your Steam login: ");
						result = Console.ReadLine();
						break;

					case SteamUserInputType.Password:
						Logger.Log("Please enter your Steam password: ");
						result = Helpers.ReadLineMasked('•');
						break;

					case SteamUserInputType.SteamGuard:
						Logger.Log("Please enter SteamGuard auth code: ");
						result = Console.ReadLine();
						break;

					case SteamUserInputType.SteamParentalPIN:
						Logger.Log("Please enter Steam parental PIN: ");
						result = Helpers.ReadLineMasked();
						break;

					case SteamUserInputType.TwoFactorAuthentication:
						Logger.Log("Please enter 2FA authenticator code: ");
						result = Console.ReadLine();
						break;

					default:
						Logger.Log("Unknown value.", LogLevels.Warn);
						break;
				}

				if (!Console.IsOutputRedirected) {
					Console.Clear();
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return null;
			}

			return !string.IsNullOrEmpty(result) ? result.Trim() : null;
		}

		public bool InitModuleService() {
			RequiresInternetConnection = true;
			SteamConfig = LoadSteamConfig();
			(bool, ConcurrentDictionary<string, ISteamBot>) result = InitSteamBots();
			if (result.Item1) {
				return true;
			}

			return false;
		}

		public bool InitModuleShutdown() {
			SaveSteamConfig(SteamConfig);
			return DisposeAllSteamBots();
		}
	}
}
