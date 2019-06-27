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
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace Steam {
	public class SteamBot : IDisposable, ISteamBot {
		public class SteamBotConfig : ISteamBotConfig {
			[JsonProperty] public string SteamID { get; set; }
			[JsonProperty] public string SteamPass { get; set; }
			[JsonProperty(Required = Required.DisallowNull)] public bool Enabled { get; set; } = true;
			[JsonProperty(Required = Required.DisallowNull)] public bool SteamChatLogger { get; set; } = true;
			[JsonProperty(Required = Required.DisallowNull)] public bool RemoveSpammers { get; set; } = false;
			[JsonProperty(Required = Required.DisallowNull)] public bool AcceptFriends { get; set; } = true;
			[JsonProperty(Required = Required.DisallowNull)] public bool DeclineGroupInvites { get; set; } = false;
			[JsonProperty] public List<string> ReplyOnAdd { get; set; }
			[JsonProperty] public List<string> ChatResponses { get; set; }
			[JsonProperty] public List<string> CustomText { get; set; }
			[JsonProperty] public HashSet<uint> GamesToPlay { get; set; } = new HashSet<uint>();
			[JsonProperty] public string SteamParentalPin { get; set; } = "0";
			[JsonProperty] public bool OfflineConnection { get; set; } = false;
			[JsonProperty(Required = Required.DisallowNull)] public Dictionary<ulong, SteamPermissionLevels> PermissionLevel { get; set; }

		}

		private Logger Logger;
		private ISteamClient SteamHandler;
		private ISteamBotConfig BotConfig;
		private SteamClient SteamClient;
		private SteamUser SteamUser;
		private SteamApps SteamApps;
		private SteamFriends SteamFriends;
		private CallbackManager CallbackManager;
		private const int LoginID = 3033;
		private DateTime LastLogonSessionReplaced;

		private string AuthCode { get; set; }
		private string TwoFactorCode { get; set; }
		private string LoginKey { get; set; }
		public bool IsBotRunning { get; set; }
		public string BotName { get; set; }
		public ulong Steam64ID { get; set; }

		private string ConfigFilePath => Constants.ConfigDirectory + "/" + BotName + ".json";

		private string SentryFilePath => Constants.ConfigDirectory + "/" + BotName + ".bin";

		public async Task<(bool, ISteamBot)> RegisterSteamBot(string botName, Logger logger, SteamClient steamClient, ISteamClient steamHandler, CallbackManager callbackManager, ISteamBotConfig botConfig) {
			BotName = botName ?? throw new ArgumentNullException();
			Logger = logger ?? throw new ArgumentNullException();
			SteamClient = steamClient ?? throw new ArgumentNullException();
			SteamHandler = steamHandler ?? throw new ArgumentNullException();
			CallbackManager = callbackManager ?? throw new ArgumentNullException();
			BotConfig = botConfig ?? throw new ArgumentNullException();

			SteamUser = SteamClient.GetHandler<SteamUser>();
			SteamApps = SteamClient.GetHandler<SteamApps>();
			SteamFriends = SteamClient.GetHandler<SteamFriends>();

			CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
			CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
			CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
			CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
			CallbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
			CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
			CallbackManager.Subscribe<SteamApps.FreeLicenseCallback>(OnFreeGameAdded);
			CallbackManager.Subscribe<SteamApps.LicenseListCallback>(OnLicenseList);
			CallbackManager.Subscribe<SteamFriends.FriendAddedCallback>(OnFriendAdded);
			CallbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessage);
			CallbackManager.Subscribe<SteamFriends.PersonaStateCallback>(OnPersonaState);

			Logger.Log("Connecting to steam...");
			await Task.Delay(500).ConfigureAwait(false);
			SteamClient.Connect();
			IsBotRunning = true;
			return (true, this);
		}

		private void OnPersonaState(SteamFriends.PersonaStateCallback callback) {

		}

		private void OnFriendMessage(SteamFriends.FriendMsgCallback callback) {

		}

		private void OnFriendAdded(SteamFriends.FriendAddedCallback callback) {

		}

		private void OnLicenseList(SteamApps.LicenseListCallback callback) {

		}

		private void OnFreeGameAdded(SteamApps.FreeLicenseCallback callback) {

		}

		private void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback) {
			int fileSize;
			byte[] sentryHash;

			try {
				using (FileStream fileStream = File.Open(SentryFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
					fileStream.Seek(callback.Offset, SeekOrigin.Begin);
					fileStream.Write(callback.Data, 0, callback.BytesToWrite);
					fileSize = (int) fileStream.Length;

					fileStream.Seek(0, SeekOrigin.Begin);
					using (SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider()) {
						sentryHash = sha.ComputeHash(fileStream);
					}
				}
			}
			catch (Exception e) {
				Logger.Log(e);

				try {
					File.Delete(SentryFilePath);
				}
				catch {
				}

				return;
			}

			SteamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails {
				JobID = callback.JobID,
				FileName = callback.FileName,
				BytesWritten = callback.BytesToWrite,
				FileSize = fileSize,
				Offset = callback.Offset,
				Result = EResult.OK,
				LastError = 0,
				OneTimePassword = callback.OneTimePassword,
				SentryFileHash = sentryHash
			});
		}

		private void OnLoginKey(SteamUser.LoginKeyCallback callback) {
			LoginKey = callback.LoginKey;
			SteamUser.AcceptNewLoginKey(callback);
			Logger.Log("Login key recevied and account authenticated!");
		}

		private void OnLoggedOff(SteamUser.LoggedOffCallback callback) {
			switch (callback.Result) {
				case EResult.LoggedInElsewhere:
					Logger.Log($"A game has been started without stopping the Boost. ({callback.Result})", LogLevels.Warn);
					break;
				case EResult.LogonSessionReplaced:
					DateTime now = DateTime.UtcNow;
					Logger.Log($"Logon Session has been forcefully Replaced. Disconnecting from Steam. ({callback.Result})", LogLevels.Warn);
					if (now.Subtract(LastLogonSessionReplaced).TotalHours < 1) {
						Logger.Log("Another TESS instance is already running the same account, therefore, this account cannot run on this instance.", LogLevels.Error);
						Stop();
						break;
					}

					LastLogonSessionReplaced = now;
					break;
			}
			SteamClient.Disconnect();
		}

		private void OnLoggedOn(SteamUser.LoggedOnCallback callback) {
			AuthCode = TwoFactorCode = null;
			switch (callback.Result) {
				case EResult.AccountLoginDeniedNeedTwoFactor:
					Logger.Log("Account is 2FA protected, Input the 2FA code: ");
					TwoFactorCode = SteamHandler.GetUserInput(SteamUserInputType.TwoFactorAuthentication);
					//TODO handle reconnect
					break;
				case EResult.AccountDisabled:
					Logger.Log("Account has been disabled for some reason, we cannot connect to steam using this account.", LogLevels.Warn);
					break;
				case EResult.AccountLogonDenied:
					Logger.Log("Account has Mail Guard protection, enter the code send to your email: ");
					AuthCode = SteamHandler.GetUserInput(SteamUserInputType.SteamGuard);
					break;
				case EResult.OK:
					Logger.Log("Successfully logged on!");
					Steam64ID = callback.ClientSteamID.ConvertToUInt64();

					if (!BotConfig.OfflineConnection) {
						SteamFriends.SetPersonaState(EPersonaState.Online);
					}

					break;
				case EResult.InvalidPassword:
					Logger.Log($"Unable to Login. Invalid Password. ({callback.Result})", LogLevels.Warn);
					break;
				case EResult.NoConnection:
				case EResult.PasswordRequiredToKickSession:
				case EResult.ServiceUnavailable:
				case EResult.Timeout:
				case EResult.TryAnotherCM:
				case EResult.RateLimitExceeded:
					Logger.Log("Unable to login, steam server might be down.", LogLevels.Warn);
					break;
			}
		}

		private void OnDisconnected(SteamClient.DisconnectedCallback callback) {
			IsBotRunning = false;
			if (callback.UserInitiated) {
				Logger.Log("Disconnected from steam. (User Initiated)");
			}
			else {
				Logger.Log("Disconnected from steam.");
			}
		}

		private async void OnConnected(SteamClient.ConnectedCallback callback) {
			Logger.Log($"Connected to steam! Logging in {BotName}");
			Regex regex = new Regex(@"[^\u0000-\u007F]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			BotConfig.SteamID = regex.Replace(BotConfig.SteamID, "");
			BotConfig.SteamPass = regex.Replace(BotConfig.SteamPass, "");

			byte[] sentryFileHash = null;

			if (File.Exists(SentryFilePath)) {
				try {
					byte[] sentryFileContent = await Compatibility.File.ReadAllBytesAsync(SentryFilePath).ConfigureAwait(false);
					sentryFileHash = CryptoHelper.SHAHash(sentryFileContent);
				}
				catch (Exception e) {
					Logger.Log(e);

					try {
						File.Delete(SentryFilePath);
					}
					catch {
					}
				}
			}

			SteamUser.LogOn(new SteamUser.LogOnDetails() {
				Username = BotConfig.SteamID,
				Password = BotConfig.SteamPass,
				ShouldRememberPassword = true,
				LoginID = LoginID,
				LoginKey = LoginKey,
				TwoFactorCode = TwoFactorCode,
				AuthCode = AuthCode,
				SentryFileHash = sentryFileHash
			});
		}

		public void Stop() {
			Logger.Log("Stopping account...");

			if (SteamClient.IsConnected) {
				SteamClient.Disconnect();
				IsBotRunning = false;
			}
		}

		public void Dispose() {
			Stop();
			SteamHandler.SaveConfig(BotName, BotConfig);
			SteamHandler.RemoveBotFromCollection(BotName);
			Logger.Log($"Disposed current bot instance of {BotConfig.SteamID}", LogLevels.Trace);
		}
	}

	public class Steam : IModuleBase, ISteamClient {
		private readonly Logger Logger = new Logger("STEAM");
		public Steam SteamInstance { get; set; }
		public bool RequiresInternetConnection { get; set; }
		public ConcurrentDictionary<string, ISteamBot> SteamBotCollection { get; set; } = new ConcurrentDictionary<string, ISteamBot>();
		private string BotConfigDirectory => Constants.ConfigDirectory + "/SteamBots/";
		public string ModuleIdentifier { get; } = nameof(Steam);
		public Version ModuleVersion { get; } = new Version("4.9.0.0");
		public string ModuleAuthor { get; } = "Arun";

		public (bool, ConcurrentDictionary<string, ISteamBot>) InitSteamBots() {
			if (!Directory.Exists(BotConfigDirectory)) {
				Logger.Log("Config directory doesn't exist!", LogLevels.Warn);
				return (false, new ConcurrentDictionary<string, ISteamBot>());
			}

			SteamBotCollection.Clear();

			try {
				List<ISteamBotConfig> botConfigs = LoadConfig();

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
							SteamConfiguration steamConfig =
								SteamConfiguration.Create(builder => builder.WithProtocolTypes(ProtocolTypes.All));
							SteamClient botClient = new SteamClient(steamConfig);
							CallbackManager manager = new CallbackManager(botClient);
							Logger BotLogger = new Logger(botName);
							SteamBot Bot = new SteamBot();

							(bool initStatus, ISteamBot botInstance) steamBotStatus = Task.Run(async () =>
								await Bot.RegisterSteamBot(botName, BotLogger, botClient, this, manager, config).ConfigureAwait(false)).Result;

							if (steamBotStatus.initStatus && steamBotStatus.botInstance != null) {
								Logger.Log($"{botName} account connected and authenticated successfully!");
								AddBotToCollection(botName, Bot);
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

		public List<ISteamBotConfig> LoadConfig() {
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

				resultConfigFiles.Add(JsonConvert.DeserializeObject<SteamBot.SteamBotConfig>(JSON));
				Logger.Log($"Loaded {resultConfigFiles[counter].SteamID} account.", LogLevels.Trace);
				counter++;
			}

			return resultConfigFiles;
		}

		public bool SaveConfig(string botName, ISteamBotConfig updatedConfig) {
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

		public void AddBotToCollection(string botName, ISteamBot bot) {
			if (SteamBotCollection.ContainsKey(botName)) {
				Logger.Log("Deleting duplicate entry of current instance.", LogLevels.Trace);
				SteamBotCollection.TryRemove(botName, out _);
			}

			SteamBotCollection.TryAdd(botName, bot);
			Logger.Log("Added current instance to client collection.", LogLevels.Trace);
		}

		public void RemoveBotFromCollection(string botName) {
			if (SteamBotCollection.ContainsKey(botName)) {
				Logger.Log("Remove current bot instance from collection.", LogLevels.Trace);
				SteamBotCollection.TryRemove(botName, out _);
			}
		}

		public bool DisposeAllBots() {
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
						result = Helpers.ReadLineMasked();
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
			(bool, ConcurrentDictionary<string, ISteamBot>) result = InitSteamBots();
			if (result.Item1) {
				SteamInstance = this;
				return true;
			}

			return false;
		}

		public bool InitModuleShutdown() => DisposeAllBots();

	}
}
