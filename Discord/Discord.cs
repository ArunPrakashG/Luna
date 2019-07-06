using Discord.Commands;
using Discord.WebSocket;
using AssistantCore;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using static AssistantCore.Enums;

namespace Discord {
	public class Discord : IModuleBase, IDiscordBot, IDiscordLogger {
		public DiscordSocketClient Client { get; set; }
		public bool RequiresInternetConnection { get; set; }
		private readonly CommandService Commands;
		private ModuleInfo DiscordModuleInfo { get; set; }
		public bool IsServerOnline { get; set; }
		public Logger Logger = new Logger("DISCORD-CLIENT");
		public IDiscordBotConfig BotConfig { get; set; }
		public long ModuleIdentifier { get; set; }
		public Version ModuleVersion { get; } = new Version("5.0.0.0");
		public string ModuleAuthor { get; } = "Arun Prakash";
		public static long ModuleIdentifierInternal { get; private set; }

		public Discord() {
			Client = new DiscordSocketClient(new DiscordSocketConfig {
				LogLevel = LogSeverity.Info,
				MessageCacheSize = 5,
				DefaultRetryMode = RetryMode.RetryRatelimit
			});

			Commands = new CommandService(new CommandServiceConfig {
				CaseSensitiveCommands = false,
				DefaultRunMode = RunMode.Async,
				LogLevel = LogSeverity.Debug,
				ThrowOnError = true
			});

			DiscordModuleInfo = Commands.AddModuleAsync<DiscordCommandBase>(null).Result;
			BotConfig = DiscordBotConfig.LoadConfig();
			ModuleIdentifierInternal = ModuleIdentifier;
		}

		public async Task<bool> StopServer() {
			if (Client != null) {
				try {
					if (Client.ConnectionState == ConnectionState.Connected || Client.ConnectionState == ConnectionState.Connecting) {
						await Client.StopAsync().ConfigureAwait(false);
					}

					while (true) {
						if (Client.ConnectionState == ConnectionState.Disconnected) {
							IsServerOnline = false;
							break;
						}
						else {
							Logger.Log("Waiting for Discord client to disconnect...", LogLevels.Trace);
							await Task.Delay(90).ConfigureAwait(false);
						}
					}

					Client?.Dispose();

					Logger.Log("Discord server stopped!");
				}
				catch (IOException io) {
					Logger.Log($"IO Exception: {io.Message}/{io.TargetSite}", LogLevels.Error);
					return false;
				}
				catch (SocketException so) {
					Logger.Log($"Socket Exception: {so.Message}/{so.TargetSite}", LogLevels.Error);
					return false;
				}
				catch (TaskCanceledException tc) {
					Logger.Log($"Task Canceled Exception: {tc.Message}/{tc.TargetSite}", LogLevels.Error);
					return false;
				}
			}

			return true;
		}

		public async Task<(bool, IDiscordBot)> RegisterDiscordClient() {
			if (BotConfig == null || !BotConfig.EnableDiscordBot) {
				return (false, this);
			}

			Logger.Log("Starting discord client...");
			Client.Log += DiscordCoreLogger;
			Client.MessageReceived += OnMessageReceived;
			Client.Ready += OnClientReady;
			int connectionTry = 1;
			while (true) {
				if (connectionTry > 5) {
					Logger.Log($"Connection failed after 5 attempts. cannot proceed to connect.", LogLevels.Error);
					IsServerOnline = false;
					return (false, this);
				}

				try {
					await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null).ConfigureAwait(false);
					await Client.LoginAsync(TokenType.Bot, BotConfig.DiscordBotToken);
					await Client.StartAsync().ConfigureAwait(false);
					IsServerOnline = true;
					Logger.Log($"Discord server is online!");
				}
				catch (HttpRequestException) {
					Logger.Log("HTTP request exception thrown.", LogLevels.Error);
				}
				catch (Exception ex) {
					Logger.Log(ex, LogLevels.Error);
				}

				if (IsServerOnline) {
					break;
				}
				else {
					if (connectionTry < 5) {
						Logger.Log($"Could not connect, retrying... ({connectionTry}/5)", LogLevels.Warn);
					}
					connectionTry++;
				}
			}

			return (true, this);
		}

		private async Task OnMessageReceived(SocketMessage message) {
			SocketUserMessage Message = message as SocketUserMessage;
			SocketCommandContext Context = new SocketCommandContext(Client, Message);
			Client = Context.Client;

			if (Context.Channel.Id.Equals(BotConfig.DiscordLogChannelID)) {
				return;
			}

			if (Message != null) {
				Logger.Log($"{Context.Message.Author.Id} | {Context.Message.Author.Username} => {Message.Content}",
					LogLevels.Trace);
			}

			if (Context.Message == null || string.IsNullOrEmpty(Context.Message.Content) || string.IsNullOrWhiteSpace(Context.Message.Content) || Context.User.IsBot) {
				return;
			}

			IResult Result = await Commands.ExecuteAsync(Context, 0, null).ConfigureAwait(false);
			await CommandExecutedResult(Result, Context).ConfigureAwait(false);
		}

		private async Task CommandExecutedResult(IResult result, SocketCommandContext context) {
			if (!result.IsSuccess) {
				switch (result.Error.ToString()) {
					case "BadArgCount":
						await context.Channel.SendMessageAsync("Your input doesn't satisfy the command syntax.").ConfigureAwait(false);
						return;

					case "UnknownCommand":
						await context.Channel.SendMessageAsync("Invalid command. Please send **!commands** for full list of valid commands!").ConfigureAwait(false);
						return;
				}

				switch (result.ErrorReason) {
					case "Command must be used in a guild channel":
						await context.Channel.SendMessageAsync("This command is supposed to be used in a channel.").ConfigureAwait(false);
						return;

					case "Invalid context for command; accepted contexts: DM":
						await context.Channel.SendMessageAsync("This command can only be used in private chat session with the bot.").ConfigureAwait(false);
						return;

					case "User requires guild permission ChangeNickname":
					case "User requires guild permission Administrator":
						await context.Channel.SendMessageAsync("Sorry, you lack the permissions required to run this command.").ConfigureAwait(false);
						return;
				}

				await LogToChannel("Error: " + context.Message.Content + " : " + result.Error + " : " + result.ErrorReason).ConfigureAwait(false);
				Logger.Log("Error: " + context.Message.Content + " : " + result.Error + " : " + result.ErrorReason, LogLevels.Warn);
			}
		}

		private async Task OnClientReady() {
			await Client.SetGameAsync("Core home assistant!", null, ActivityType.Playing).ConfigureAwait(false);
			await LogToChannel($"{Core.AssistantName} discord command bot is ready!").ConfigureAwait(false);
		}

		//Discord Core Logger
		//Not to be confused with HomeAssistant Discord channel logger
		private async Task DiscordCoreLogger(LogMessage message) {
			if (!BotConfig.EnableDiscordBot || !BotConfig.DiscordLog || !IsServerOnline) {
				return;
			}

			await Task.Delay(100).ConfigureAwait(false);
			switch (message.Severity) {
				case LogSeverity.Critical:
				case LogSeverity.Error:
					Logger.Log(message.Message, LogLevels.Warn);
					break;
				case LogSeverity.Warning:
				case LogSeverity.Verbose:
				case LogSeverity.Debug:
				case LogSeverity.Info:
					Logger.Log(message.Message, LogLevels.Trace);
					break;

				default:
					goto case LogSeverity.Info;
			}
		}

		public async Task RestartDiscordServer() {
			try {
				_ = await StopServer().ConfigureAwait(false);

				Client?.Dispose();

				await Task.Delay(5000).ConfigureAwait(false);
				await RegisterDiscordClient().ConfigureAwait(false);
			}
			catch (Exception) {
				throw;
			}
		}

		public bool InitModuleService() {
			RequiresInternetConnection = true;
			if (RegisterDiscordClient().Result.Item1) {
				return true;
			}

			return false;
		}

		public bool InitModuleShutdown() {
			DiscordBotConfig.SaveConfig(BotConfig);
			return StopServer().Result;
		}

		private string LogOutputFormat(string message) {
			string shortDate = DateTime.Now.ToShortDateString();
			string shortTime = DateTime.Now.ToShortTimeString();
			return $"{shortDate} : {shortTime} | {message}";
		}

		public async Task LogToChannel(string message) {
			if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)) {
				return;
			}

			if (!Core.CoreInitiationCompleted || !BotConfig.DiscordLog || !Core.IsNetworkAvailable || Core.ModuleLoader.LoadedModules.DiscordBots == null || Core.ModuleLoader.LoadedModules.DiscordBots.Count <= 0) {
				return;
			}

			if (!IsServerOnline) {
				return;
			}

			string LogOutput = LogOutputFormat(message);

			try {
				SocketGuild Guild = Client?.Guilds?.FirstOrDefault(x => x.Id == BotConfig.DiscordServerID);
				SocketTextChannel Channel = Guild?.Channels?.FirstOrDefault(x => x.Id == BotConfig.DiscordLogChannelID) as SocketTextChannel;
				if (Guild != null || Channel != null) {
					await Channel.SendMessageAsync(LogOutput).ConfigureAwait(false);
				}
			}
			catch (NullReferenceException) {
				Logger.Log("Null reference exception thrown. possibly, client is null.", LogLevels.Trace);
				return;
			}
			catch (ArgumentOutOfRangeException) {
				Logger.Log($"The message to send has charecters more than 2000 which is discord limit. ({message.Length} charecters)", LogLevels.Trace);
				return;
			}
			catch (ArgumentException) {
				Logger.Log($"One of the arguments provided is null or unknown.", LogLevels.Trace);
				return;
			}
			catch (TaskCanceledException) {
				Logger.Log("A task has been cancelled by waiting.", LogLevels.Trace);
				return;
			}
		}
	}
}
