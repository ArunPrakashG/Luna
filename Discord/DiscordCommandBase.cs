using Discord.Commands;
using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.RaspberryIO.Abstractions;
using Assistant.AssistantCore;

namespace Discord {

	public class DiscordCommandBase : ModuleBase<SocketCommandContext> {
		private int ArgPos = 0;
		private readonly Logger Logger = new Logger("DISCORD-BASE");
		private ulong OwnerID = 0;

		private bool IsAllowed(ulong id) {
			if (OwnerID == 0 && Core.ModuleLoader != null && Core.ModuleLoader.LoadedModules != null &&
				Core.ModuleLoader.LoadedModules.DiscordBots.Count > 0) {
				foreach (((Enums.ModuleType, string), Assistant.Modules.Interfaces.IDiscordBot) bot in Core.ModuleLoader.LoadedModules.DiscordBots) {
					if (bot.Item1.Equals(Discord.ModuleIdentifierInternal)) {
						OwnerID = bot.Item2.BotConfig.DiscordOwnerID;
					}
				}
			}

			if (id.Equals(OwnerID)) {
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
					Logger.Log("The Content is above 2000 words which is not allowed.", Enums.LogLevels.Warn);
					return false;
				}
				else {
					Logger.Log($"Exception Thrown: {ex.InnerException} / {ex.Message} / {ex.TargetSite}", Enums.LogLevels.Warn);
					return false;
				}
			}
			return true;
		}

		[Command("!commands"), Summary("Commands List")]
		public async Task Help() {
			EmbedBuilder builder = new EmbedBuilder();
			builder.WithTitle($" **=> {Core.AssistantName.ToUpper()} HOME ASSISTANT COMMANDS <=** ");
			builder.AddField("**!relay1**", "turn on light 1, send again to turn off");
			builder.AddField("**!relay2**", "turn on light 2, send again to turn off");
			builder.AddField("**!relay3**", "turn on light 3, send again to turn off");
			builder.AddField("**!relay4**", "turn on light 4, send again to turn off");
			builder.AddField("**!relay5**", "turn on light 5, send again to turn off");
			builder.AddField("**!relay6**", "turn on light 6, send again to turn off");
			builder.AddField("**!relay7**", "turn on light 7, send again to turn off");
			builder.AddField("**!relay8**", "turn on light 8, send again to turn off");
			builder.AddField("**!timedtask**", "[int relaynumber] [int delayinMinutes] [int pinstatus] run a task with a delay.");
			builder.AddField("**!exit**", "Exit assistant");
			builder.AddField("**!shutdown**", "Shutdown raspberry pi");
			builder.AddField("**!restart**", "Restart raspberry pi");
			builder.AddField("**!relaycycle**", "cycle throught each relay channels, for testing purposes.");
			await ReplyAsync("", false, builder.Build()).ConfigureAwait(false);
		}

		[Command("!relay1")]
		public async Task Light1() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[0]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[0]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[0], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[0]} pin to ON.");
			}
		}

		[Command("!relay2")]
		public async Task Light2() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[1]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[1]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[1], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[1]} pin to ON.");
			}
		}

		[Command("!relay3")]
		public async Task Light3() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[2]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[2]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[2], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[2]} pin to ON.");
			}
		}

		[Command("!relay4")]
		public async Task Light4() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[3]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[3]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[3], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[3]} pin to ON.");
			}
		}

		[Command("!relay5")]
		public async Task Light5() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[4]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[4]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[4], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[3]} pin to ON.");
			}
		}

		[Command("!relay6")]
		public async Task Light6() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[5]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[5]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[5], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[6]} pin to ON.");
			}
		}

		[Command("!relay7")]
		public async Task Light7() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[6]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[6]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[6], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[5]} pin to ON.");
			}
		}

		[Command("!relay8")]
		public async Task Light8() {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(Core.Config.RelayPins[7]);

			if (PinStatus.IsOn) {
				Core.Controller.SetGPIO(Core.Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.High);
				await Response($"Sucessfully set {Core.Config.RelayPins[7]} pin to OFF.");
			}
			else {
				Core.Controller.SetGPIO(Core.Config.RelayPins[7], GpioPinDriveMode.Output, GpioPinValue.Low);
				await Response($"Sucessfully set {Core.Config.RelayPins[7]} pin to ON.");
			}
		}

		[Command("!relaycycle")]
		public async Task RelayCycle([Remainder] int cycleMode = 0) {
			if (!IsAllowed(Context.User.Id)) {
				await Response("Sorry, you are not allowed to execute this command.").ConfigureAwait(false);
				return;
			}

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
				return;
			}

			async void action() {
				switch (cycleMode) {
					case 0:
						if (await Core.Controller.RelayTestService(Enums.GPIOCycles.OneMany).ConfigureAwait(false)) {
							await Response("OneMany relay test completed!").ConfigureAwait(false);
						}
						break;

					case 1:
						if (await Core.Controller.RelayTestService(Enums.GPIOCycles.OneOne).ConfigureAwait(false)) {
							await Response("OneOne relay test completed!").ConfigureAwait(false);
						}
						break;

					case 2:
						if (await Core.Controller.RelayTestService(Enums.GPIOCycles.OneTwo).ConfigureAwait(false)) {
							await Response("OneTwo relay test completed!").ConfigureAwait(false);
						}
						break;

					case 3:
						if (await Core.Controller.RelayTestService(Enums.GPIOCycles.Cycle).ConfigureAwait(false)) {
							await Response("Cycle relay test completed!").ConfigureAwait(false);
						}
						break;

					case 4:
						if (await Core.Controller.RelayTestService(Enums.GPIOCycles.Base).ConfigureAwait(false)) {
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

			if (Core.DisablePiMethods) {
				await Response("Sorry, assistant is running on unknown OS therefor all GPIO pin related methods are disabled.").ConfigureAwait(false);
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

			if (Core.Config.IRSensorPins.Contains(relaypinNumber)) {
				await Response("Sorry, the specified pin is pre-configured for IR Sensor. cannot modify!").ConfigureAwait(false);
				return;
			}

			if (!Core.Config.RelayPins.Contains(relaypinNumber)) {
				await Response("Sorry, the specified pin doesn't exist in the relay pin catagory.").ConfigureAwait(false);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(relaypinNumber);

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
					Core.Controller.SetGPIO(relaypinNumber, GpioPinDriveMode.Output, GpioPinValue.High);
					await Response($"Sucessfully finished execution of the task: {relaypinNumber} pin set to OFF.");
				}

				if (!PinStatus.IsOn && pinStatus.Equals(1)) {
					Core.Controller.SetGPIO(relaypinNumber, GpioPinDriveMode.Output, GpioPinValue.Low);
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
		public async Task AssistantExit(int delay = 5) {
			await Response($"Exiting in {delay} seconds").ConfigureAwait(false);
			Helpers.ScheduleTask(async () => await Core.Exit(0).ConfigureAwait(false), TimeSpan.FromSeconds(delay));
		}

		[Command("!shutdown"), RequireOwner]
		public async Task PiShutdown(int delay = 10) {
			await Response($"Exiting in {delay} seconds").ConfigureAwait(false);
			Helpers.ScheduleTask(() => Helpers.ExecuteCommand("sudo shutdown -h now"), TimeSpan.FromSeconds(delay));
		}

		[Command("!restart"), RequireOwner]
		public async Task AssistantRestart(int delay = 8) {
			await Response($"Restarting in {delay} seconds").ConfigureAwait(false);
			await Core.Restart(8);
		}
	}
}
