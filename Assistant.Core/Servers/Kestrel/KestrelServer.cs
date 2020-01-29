using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Servers.Kestrel.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.Servers.Kestrel {
	public static class KestrelServer {
		private static IHost? KestrelWebHost;
		private static readonly Logger Logger = new Logger("KESTREL-SERVER");
		public static UserAuthentication Authentication { get; set; } = new UserAuthentication();
		public static bool IsServerOnline;

		public static async Task Start() {
			if (KestrelWebHost != null) {
				return;
			}

			Logger.Log("Starting Kestrel HTTP server...", Enums.LogLevels.Trace);
			HostBuilder builder = new HostBuilder();
			string absoluteConfigDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.ConfigDirectory);
			builder.ConfigureLogging(logging => logging.SetMinimumLevel(Core.Config.Debug ? LogLevel.Trace : LogLevel.Warning));

			builder.ConfigureWebHostDefaults(
				webBuilder => {
					webBuilder.UseKestrel(options => options.ListenAnyIP(Core.Config.KestrelServerPort));
					webBuilder.UseStartup<Startup>();
				}
			);

			builder.UseNLog();
			IHost kestrelWebHost = builder.Build();

			try {
				await kestrelWebHost.StartAsync().ConfigureAwait(false);
			}
			catch (Exception e) {
				Logger.Log(e);
				kestrelWebHost?.Dispose();
				IsServerOnline = false;
				return;
			}

			KestrelWebHost = kestrelWebHost;
			IsServerOnline = true;
			Logger.Log($"Kestrel server is running at http://{Constants.LocalIP}:{Core.Config.KestrelServerPort}/ ");
		}

		public static async Task Stop() {
			if (KestrelWebHost == null) {
				return;
			}

			await KestrelWebHost.StopAsync().ConfigureAwait(false);
			KestrelWebHost?.Dispose();
			IsServerOnline = false;
			KestrelWebHost = null;
			Logger.Log("Kestrel server is offline.");
		}
	}
}
