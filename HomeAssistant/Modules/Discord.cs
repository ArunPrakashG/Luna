using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unosquare.RaspberryIO.Abstractions;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {

	public class DiscordClient {
		public DiscordSocketClient Client;
		private readonly CommandService Commands;
		public bool IsServerOnline;
		internal Logger Logger;
		internal CoreConfig Config = Program.Config;
		private readonly DiscordLogger DiscordLogger;
		private readonly string DiscordToken;

		public DiscordClient() {
			Logger = new Logger("DISCORD-CLIENT");

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

			DiscordToken = Helpers.FetchVariable(2, true, "DISCORD_TOKEN");
			DiscordLogger = new DiscordLogger(Client, "DISCORD-CLIENT");
		}

		public async Task StopServer() {
			if (Client != null) {
				await Client.StopAsync().ConfigureAwait(false);
			}
			IsServerOnline = false;
			Logger.Log("Discord server stopped!");
		}

		public async Task<bool> InitDiscordClient() {
			if (!Config.DiscordBot) {
				return false;
			}

			Logger.Log("Starting discord client...");
			Client.Log += DiscordCoreLogger;
			Client.MessageReceived += OnMessageReceived;
			Client.Ready += OnClientReady;

			try {
				await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null).ConfigureAwait(false);
				await Client.LoginAsync(TokenType.Bot, DiscordToken);
				await Client.StartAsync().ConfigureAwait(false);
				IsServerOnline = true;
				Logger.Log($"Discord server is online!");
			}
			catch (Exception ex) {
				Logger.Log(ex, ExceptionLogLevels.Error);
				return false;
			}
			return true;
		}

		private async Task OnMessageReceived(SocketMessage message) {
			SocketUserMessage Message = message as SocketUserMessage;
			SocketCommandContext Context = new SocketCommandContext(Client, Message);
			Client = Context.Client;

			if (Context.Channel.Id.Equals(Program.Config.DiscordLogChannelID)) {
				return;
			}

			Logger.Log($"{Context.Message.Author.Id} | {Context.Message.Author.Username} => {Message.Content}", LogLevels.Trace);

			if (Context.Message == null || string.IsNullOrEmpty(Context.Message.Content) || string.IsNullOrWhiteSpace(Context.Message.Content) || Context.User.IsBot) {
				return;
			}

			Helpers.InBackgroundThread(async () => {
				IResult Result = await Commands.ExecuteAsync(Context, 0, null).ConfigureAwait(false);
				await CommandExecutedResult(Result, Context).ConfigureAwait(false);
			}, "Discord Message Handler");

			await Task.Delay(10).ConfigureAwait(false);
		}

		private async Task CommandExecutedResult(IResult result, SocketCommandContext context) {
			if (!result.IsSuccess) {
				switch (result.Error.ToString()) {
					case "BadArgCount":
						await context.Channel.SendMessageAsync("Your Input doesn't satisfy the command Syntax.").ConfigureAwait(false);
						return;

					case "UnknownCommand":
						await context.Channel.SendMessageAsync("Unknown Command. Please send **!commands** for full list of valid commands!").ConfigureAwait(false);
						return;
				}

				switch (result.ErrorReason) {
					case "Command must be used in a guild channel":
						await context.Channel.SendMessageAsync("This command is supposed to be used in a Channel.").ConfigureAwait(false);
						return;

					case "Invalid context for command; accepted contexts: DM":
						await context.Channel.SendMessageAsync("This command can only be used in Private Chat Session with the BoT.").ConfigureAwait(false);
						return;

					case "User requires guild permission ChangeNickname":
					case "User requires guild permission Administrator":
						await context.Channel.SendMessageAsync("Sorry, You lack the permissions required to run this command.").ConfigureAwait(false);
						return;
				}

				await DiscordLogger.LogToChannel("Error: " + context.Message.Content + " : " + result.Error + " : " + result.ErrorReason).ConfigureAwait(false);
				Logger.Log("Error: " + context.Message.Content + " : " + result.Error + " : " + result.ErrorReason, LogLevels.Warn);
			}
		}

		private async Task OnClientReady() {
			await Client.SetGameAsync("Tess home assistant!", null, ActivityType.Playing).ConfigureAwait(false);
			await DiscordLogger.LogToChannel("TESS discord command bot is ready!").ConfigureAwait(false);
		}

		//Discord Core Logger
		//Not to be confused with HomeAssistant Discord channel logger
		public async Task DiscordCoreLogger(LogMessage message) {
			if (!Program.Config.DiscordBot || !Program.Config.DiscordLog || !Program.Modules.Discord.IsServerOnline) {
				return;
			}

			await Task.Delay(100).ConfigureAwait(false);
			switch (message.Severity) {
				case LogSeverity.Critical:
				case LogSeverity.Error:
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

		private async Task RestartDiscordServer() {
			try {
				await StopServer().ConfigureAwait(false);

				if (Client != null) {
					Client.Dispose();
				}

				await Task.Delay(5000).ConfigureAwait(false);
				await InitDiscordClient().ConfigureAwait(false);
			}
			catch (Exception) {
				throw;
			}
		}
	}

	public sealed class DiscordCommandBase : ModuleBase<SocketCommandContext> {
		private int ArgPos = 0;
		private readonly Logger Logger = new Logger("DISCORD-BASE");

		private bool IsAllowed(ulong id) {
			if (id.Equals(Program.Config.DiscordOwnerID)) {
				return true;
			}

			if (!Context.Message.HasCharPrefix('!', ref ArgPos) || Context.User.IsBot) {
				return false;
			}

			return true;
		}

		private async Task<bool> Response(string response) {
			if (string.IsNullOrEmpty(response) || string.IsNullOrWhiteSpace(response)) {
				Logger.Log("Response text is null!");
				return false;
			}

			try {
				await ReplyAsync($"`{response}`").ConfigureAwait(false);
			}
			catch (Exception ex) {
				if (ex is ArgumentException || ex is ArgumentOutOfRangeException) {
					Logger.Log("The Content is above 2000 words which is not allowed.", LogLevels.Warn);
					return false;
				}
				else {
					Logger.Log($"Exception Thrown: {ex.InnerException} / {ex.Message} / {ex.TargetSite}", LogLevels.Warn);
					return false;
				}
			}
			return true;
		}

		[Command("!commands"), Summary("Commands List")]
		public async Task Help() {
			EmbedBuilder builder = new EmbedBuilder();
			builder.WithTitle(" **=> TESS HOME ASSISTANT COMMANDS <=** ");
			builder.AddField("**!light1**", "turn on light 1, send again to turn off");
			builder.AddField("**!light2**", "turn on light 2, send again to turn off");
			builder.AddField("**!light3**", "turn on light 3, send again to turn off");
			builder.AddField("**!light4**", "turn on light 4, send again to turn off");
			builder.AddField("**!light5**", "turn on light 5, send again to turn off");
			builder.AddField("**!light6**", "turn on light 6, send again to turn off");
			builder.AddField("**!light7**", "turn on light 7, send again to turn off");
			builder.AddField("**!light8**", "turn on light 8, send again to turn off");
			builder.AddField("**!relaycycle**", "cycle throught each relay channels, for testing purposes.");
			await ReplyAsync("", false, builder.Build()).ConfigureAwait(false);
		}

		[Command("!relay1")]
		public async Task Light1() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[0]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[0]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[0]} pin to ON.");
			}
		}

		[Command("!relay2")]
		public async Task Light2() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[1]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[1]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[1]} pin to ON.");
			}
		}

		[Command("!relay3")]
		public async Task Light3() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[2]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[2]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[2]} pin to ON.");
			}
		}

		[Command("!relay4")]
		public async Task Light4() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[3]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[3]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[3]} pin to ON.");
			}
		}

		[Command("!relay5")]
		public async Task Light5() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[4]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[4]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[3]} pin to ON.");
			}
		}

		[Command("!relay6")]
		public async Task Light6() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[5]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[5]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[6]} pin to ON.");
			}
		}

		[Command("!relay7")]
		public async Task Light7() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[6]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[6]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[5]} pin to ON.");
			}
		}

		[Command("!relay8")]
		public async Task Light8() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(Program.Config.RelayPins[7]);

			if (PinStatus.IsOn) {
				Program.Controller.SetGPIO(Program.Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Program.Config.RelayPins[7]} pin to OFF.");
			}
			else {
				Program.Controller.SetGPIO(Program.Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Program.Config.RelayPins[7]} pin to ON.");
			}
		}

		[Command("!relaycycle")]
		public async Task RelayCycle([Remainder] int cycleMode = 0) {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			async void action() {
				switch (cycleMode) {
					case 0:
						if (await Program.Controller.RelayTestService(GPIOCycles.OneMany).ConfigureAwait(false)) {
							await Response("OneMany relay test completed!").ConfigureAwait(false);
						}
						break;

					case 1:
						if (await Program.Controller.RelayTestService(GPIOCycles.OneOne).ConfigureAwait(false)) {
							await Response("OneOne relay test completed!").ConfigureAwait(false);
						}
						break;

					case 2:
						if (await Program.Controller.RelayTestService(GPIOCycles.OneTwo).ConfigureAwait(false)) {
							await Response("OneTwo relay test completed!").ConfigureAwait(false);
						}
						break;

					case 3:
						if (await Program.Controller.RelayTestService(GPIOCycles.Cycle).ConfigureAwait(false)) {
							await Response("Cycle relay test completed!").ConfigureAwait(false);
						}
						break;

					case 4:
						if (await Program.Controller.RelayTestService(GPIOCycles.Base).ConfigureAwait(false)) {
							await Response("Base relay test completed!").ConfigureAwait(false);
						}
						break;
				}
			}

			Helpers.InBackgroundThread(action, "Relay Cycle");
		}

		[Command("!timedtask")]
		public async Task DelayedTask(int relaypinNumber, int delayInMinutes = 1, int pinStatus = 1) {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			//0 = off
			//1 = on

			if (relaypinNumber <= 0) {
				await Response("Please enter a valid relay pin number...").ConfigureAwait(false);
				return;
			}

			if (delayInMinutes <= 0) {
				await Response("Please enter a valid delay... (in minutes)").ConfigureAwait(false);
				return;
			}

			if (pinStatus != 0 && pinStatus != 1) {
				await Response("Please enter a valid pin state. (0 for off and 1 for on)").ConfigureAwait(false);
				return;
			}

			if (Program.Config.IRSensorPins.Contains(relaypinNumber)) {
				await Response("Sorry, the specified pin is pre-configured for IR Sensor. cannot modify!").ConfigureAwait(false);
				return;
			}

			if (!Program.Config.RelayPins.Contains(relaypinNumber)) {
				await Response("Sorry, the specified pin doesn't exist in the relay pin catagory.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Program.Controller.FetchPinStatus(relaypinNumber);

			if (PinStatus.IsOn && pinStatus.Equals(1)) {
				await Response("Pin is already configured to be in ON State. Command doesn't make any sense.").ConfigureAwait(false);
				return;
			}

			if (!PinStatus.IsOn && pinStatus.Equals(0)) {
				await Response("Pin is already configured to be in OFF State. Command doesn't make any sense.").ConfigureAwait(false);
				return;
			}

			Helpers.ScheduleTask(async () => {
				if (PinStatus.IsOn && pinStatus.Equals(0)) {
					Program.Controller.SetGPIO(relaypinNumber, GpioPinDriveMode.Output, GpioPinValue.High);
					await Response($"Sucessfully finished execution of the task: {relaypinNumber} pin set to OFF.");
				}

				if (!PinStatus.IsOn && pinStatus.Equals(1)) {
					Program.Controller.SetGPIO(relaypinNumber, GpioPinDriveMode.Output, GpioPinValue.Low);
					await Response($"Sucessfully finished execution of the task: {relaypinNumber} pin set to ON.");
				}
			}, TimeSpan.FromMinutes(delayInMinutes));

			if (pinStatus.Equals(0)) {
				await Response($"Successfully scheduled a task: set {relaypinNumber} pin to OFF").ConfigureAwait(false);
			}
			else {
				await Response($"Successfully scheduled a task: set {relaypinNumber} pin to ON").ConfigureAwait(false);
			}
		}

		[Command("!exit"), RequireOwner]
		public async Task TessExit(int delay = 5) {
			await Response($"Exiting in {delay} seconds").ConfigureAwait(false);
			Helpers.ScheduleTask(async () => await Program.Exit(0).ConfigureAwait(false), TimeSpan.FromSeconds(delay));
		}

		[Command("!shutdown"), RequireOwner]
		public async Task PiShutdown(int delay = 10) {
			await Response($"Exiting in {delay} seconds").ConfigureAwait(false);
			Helpers.ScheduleTask(() => Helpers.ExecuteCommand("sudo shutdown -h now"), TimeSpan.FromSeconds(delay));
		}

		[Command("!restart"), RequireOwner]
		public async Task TessRestart(int delay = 8) {
			await Response($"Restarting in {delay} seconds").ConfigureAwait(false);
			await Program.Restart(8);
		}
	}

	public class DiscordLogger {
		private readonly DiscordSocketClient Client;
		private readonly Logger Logger = new Logger("DISCORD-LOGGER");
		private readonly string LogIdentifier;

		public DiscordLogger(DiscordSocketClient client, string logIdentifier) {
			Client = client;
			LogIdentifier = logIdentifier;
		}

		private string LogOutputFormat(string message) {
			string shortDate = DateTime.Now.ToShortDateString();
			string shortTime = DateTime.Now.ToShortTimeString();
			return $"{shortDate} : {shortTime} | {LogIdentifier} | {message}";
		}

		public async Task LogToChannel(string message) {
			string LogOutput = LogOutputFormat(message);

			if (!Program.Config.DiscordBot || !Program.Config.DiscordLog || !Program.Modules.Discord.IsServerOnline || Client == null) {
				return;
			}

			try {
				SocketGuild Guild = Client.Guilds.Where(x => x.Id == Program.Config.DiscordServerID).FirstOrDefault();
				SocketTextChannel Channel = Guild.Channels.Where(x => x.Id == Program.Config.DiscordLogChannelID).FirstOrDefault() as SocketTextChannel;
				await Channel.SendMessageAsync(LogOutput).ConfigureAwait(false);
			}
			catch (Exception ex) {
				if (ex is ArgumentException || ex is ArgumentOutOfRangeException) {
					Logger.Log("The Content is above 2000 words which is not allowed.", LogLevels.Trace);
					return;
				}
				else {
					Logger.Log(ex.ToString(), LogLevels.Trace);
					return;
				}
			}
		}
	}
}
