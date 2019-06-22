using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAssistant.Modules.Interfaces;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");
		public DiscordClient Discord { get; set; }
		public GoogleMap Map { get; set; }
		public Youtube Youtube { get; set; }
		public Email Mail { get; set; }

		public (DiscordClient, Email, GoogleMap, Youtube) StartModules() {
			if (Tess.Config.DiscordBot) {
				Discord = new DiscordClient();
				(bool, DiscordClient) discordResult = Task.Run(async () => await Discord.RegisterDiscordClient().ConfigureAwait(false)).Result;
			}

			if (Tess.Config.EnableEmail) {
				Mail = new Email();
				(bool, ConcurrentDictionary<string, EmailBot>) emailResult = Mail.InitEmailBots();
			}

			if (Tess.Config.EnableGoogleMap) {
				Map = new GoogleMap();
			}

			if (Tess.Config.EnableYoutube) {
				Youtube = new Youtube();
			}
			
			return (Discord ?? null, Mail ?? null, Map ?? null, Youtube ?? null);
		}

		public bool OnCoreShutdown() {
			if (Discord != null && (Discord.Client != null || Discord.IsServerOnline)) {
				Logger.Log("Discord server shutting down...", LogLevels.Trace);
				_ = Discord.StopServer().Result;
			}

			if (Mail != null && Mail.EmailClientCollection.Count > 0) {
				Logger.Log("Email clients shutting down...", LogLevels.Trace);
				Mail.DisposeAllEmailBots();
			}

			Logger.Log("Module shutdown sucessfull.", LogLevels.Trace);
			return true;
		}
	}
}
