
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

using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Modules.Interfaces;
using SteamKit2;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Steam {
	public sealed class Bot : IDisposable, ISteamBot {
		private Logger Logger;
		private ISteamClient SteamHandler;
		public ISteamBotConfig BotConfig { get; private set; }
		public SteamClient SteamClient { get; private set; }
		public SteamUser SteamUser { get; private set; }
		public SteamApps SteamApps { get; private set; }
		public SteamFriends SteamFriends { get; private set; }
		public CallbackManager CallbackManager { get; private set; }
		private const int LoginID = 3033;
		private DateTime LastLogonSessionReplaced;
		private int DisconnectCounter { get; set; }
		private int MaxDisconnectsBeforeSleep => SteamHandler.SteamConfig.MaxDisconnectsBeforeSleep;
		private int DisconnectSleepDurationInMinutes => SteamHandler.SteamConfig.DisconnectSleepDelay;

		private string AuthCode { get; set; }
		private string TwoFactorCode { get; set; }
		private string LoginKey => BotConfig.LoginKey;
		public bool IsBotRunning { get; set; }
		public string BotName { get; set; }
		public ulong CachedSteamId { get; set; } = 0;
		public bool KeepRunning { get; set; }
		private bool IsAccountLimited(SteamUser.LoggedOnCallback callback) => callback.AccountFlags.HasFlag(EAccountFlags.LimitedUser) || callback.AccountFlags.HasFlag(EAccountFlags.LimitedUserForce);
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
			Start();
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
			BotConfig.LoginKey = callback.LoginKey;
			SteamUser.AcceptNewLoginKey(callback);
			Logger.Log("Login key recevied and account authenticated!");
		}

		private void OnLoggedOff(SteamUser.LoggedOffCallback callback) {
			switch (callback.Result) {
				case EResult.LoggedInElsewhere:
					Logger.Log($"A game has been started without stopping the Boost. ({callback.Result})", Enums.LogLevels.Warn);
					break;

				case EResult.LogonSessionReplaced:
					DateTime now = DateTime.UtcNow;
					Logger.Log($"Logon Session has been forcefully Replaced. Disconnecting from Steam. ({callback.Result})", Enums.LogLevels.Warn);
					if (now.Subtract(LastLogonSessionReplaced).TotalHours < 1) {
						Logger.Log($"Another {Core.AssistantName} instance is already running the same account, therefore, this account cannot run on this instance.", Enums.LogLevels.Error);
						Stop();
						break;
					}

					LastLogonSessionReplaced = now;
					break;
			}
			SteamClient.Disconnect();
		}

		private void OnLoggedOn(SteamUser.LoggedOnCallback callback) {
			if (callback == null) {
				Logger.Log(nameof(callback), LogLevels.Error);
				return;
			}

			AuthCode = TwoFactorCode = null;
			switch (callback.Result) {
				case EResult.AccountLoginDeniedNeedTwoFactor:
					Logger.Log("Account is 2FA protected, Input the 2FA code: ");
					TwoFactorCode = SteamHandler.GetUserInput(Enums.SteamUserInputType.TwoFactorAuthentication);

					if (Helpers.IsNullOrEmpty(TwoFactorCode)) {
						Stop();
						break;
					}

					break;

				case EResult.AccountDisabled:
					Logger.Log("Account has been disabled for some reason, we cannot connect to steam using this account.", Enums.LogLevels.Warn);
					break;

				case EResult.AccountLogonDenied:
					Logger.Log("Account has Mail Guard protection, enter the code send to your email: ");
					AuthCode = SteamHandler.GetUserInput(Enums.SteamUserInputType.SteamGuard);

					if (Helpers.IsNullOrEmpty(AuthCode)) {
						Stop();
						break;
					}

					break;

				case EResult.OK:
					Logger.Log("Successfully logged on!");
					CachedSteamId = callback.ClientSteamID.ConvertToUInt64();

					if (!BotConfig.OfflineConnection) {
						SteamFriends.SetPersonaState(EPersonaState.Online);
					}

					if (IsAccountLimited(callback)) {
						Logger.Log("The bot has a limited account.");
					}

					if (callback.CellID != 0 && callback.CellID != SteamHandler.SteamCellID) {
						SteamHandler.SteamCellID = callback.CellID;
					}

					break;

				case EResult.InvalidPassword:
					Logger.Log($"Unable to Login. Invalid Password. ({callback.Result})", Enums.LogLevels.Warn);
					break;

				case EResult.NoConnection:
				case EResult.PasswordRequiredToKickSession:
				case EResult.ServiceUnavailable:
				case EResult.Timeout:
				case EResult.TryAnotherCM:
				case EResult.RateLimitExceeded:
					Logger.Log("Unable to login, steam servers might be down.", Enums.LogLevels.Warn);
					break;
			}
		}

		private void OnDisconnected(SteamClient.DisconnectedCallback callback) {
			if (callback == null) {
				Logger.Log(nameof(callback), LogLevels.Error);
				return;
			}

			IsBotRunning = false;

			if (callback.UserInitiated) {
				Logger.Log("Disconnected from steam. (User Initiated)");
			}
			else {
				Logger.Log("Disconnected from steam.");
				DisconnectCounter++;
			}

			if (DisconnectCounter > MaxDisconnectsBeforeSleep && !callback.UserInitiated) {
				Stop();
				Logger.Log($"Sleeping for {DisconnectSleepDurationInMinutes} minutes due to multiple disconnects in short time. ({DisconnectCounter} disconnects)");
				Helpers.ScheduleTask(() => Start(), TimeSpan.FromMinutes(DisconnectSleepDurationInMinutes));
			}
		}

		private async void OnConnected(SteamClient.ConnectedCallback callback) {
			if (callback == null) {
				Logger.Log(nameof(callback), LogLevels.Error);
				return;
			}

			if (!KeepRunning) {
				Logger.Log("Disconnecting...");
				Stop();
				return;
			}

			Logger.Log($"Connected to steam! Logging in {BotName}...");

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

		public void Start() {
			if (!KeepRunning) {
				KeepRunning = true;
			}

			Logger.Log("Starting account...");

			Connect();
			IsBotRunning = true;
		}

		private void Connect() {
			if (!KeepRunning || SteamClient.IsConnected) {
				return;
			}

			Logger.Log("Connecting to steam network...");
			SteamClient.Connect();
		}

		public void Dispose() {
			Stop();
			SteamHandler.SaveSteamBotConfig(BotName, BotConfig);
			SteamHandler.RemoveSteamBotFromCollection(BotName);
			Logger.Log($"Disposed current bot instance of {BotConfig.SteamID}", Enums.LogLevels.Trace);
		}
	}
}
