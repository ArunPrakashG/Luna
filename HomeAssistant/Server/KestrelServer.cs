using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HomeAssistant.Server {

	public static class KestrelServer {
		private static IWebHost KestrelWebHost;
		private static readonly Logger Logger = new Logger("KESTREL");
		public static bool IsServerOnline;

		public static async Task Start() {
			if (KestrelWebHost != null) {
				return;
			}

			Logger.Log("Starting Kestrel IPC server...");
			IWebHostBuilder builder = new WebHostBuilder();
			string absoluteConfigDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.ConfigDirectory);
			builder.ConfigureLogging(logging => logging.SetMinimumLevel(Tess.Config.Debug ? LogLevel.Trace : LogLevel.Warning));
			if (File.Exists(Path.Combine(absoluteConfigDirectory, Constants.KestrelConfigurationFile))) {
				builder.UseConfiguration(new ConfigurationBuilder().SetBasePath(absoluteConfigDirectory).AddJsonFile(Constants.KestrelConfigurationFile, false, true).Build());
				builder.UseKestrel((builderContext, options) => options.Configure(builderContext.Configuration.GetSection("Kestrel")));
				builder.ConfigureLogging((hostingContext, logging) => logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging")));
			}
			else {
				builder.UseKestrel(options => {
					if (Tess.Config.ListernLocalHostOnly) {
						options.ListenLocalhost(Tess.Config.KestrelServerPort);
					}
					else {
						options.ListenAnyIP(Tess.Config.KestrelServerPort);
					}
				});
			}

			builder.UseNLog();
			builder.UseStartup<Startup>();
			IWebHost kestrelWebHost = builder.Build();

			try {
				await kestrelWebHost.StartAsync().ConfigureAwait(false);
			}
			catch (Exception e) {
				Logger.Log(e);
				kestrelWebHost.Dispose();
				IsServerOnline = false;
				return;
			}

			KestrelWebHost = kestrelWebHost;
			IsServerOnline = true;
			Logger.Log($"Kestrel server is running at http://localhost:{Tess.Config.KestrelServerPort}/");
		}

		public static async Task Stop() {
			if (KestrelWebHost == null) {
				return;
			}

			await KestrelWebHost.StopAsync().ConfigureAwait(false);
			KestrelWebHost.Dispose();
			IsServerOnline = false;
			KestrelWebHost = null;
			Logger.Log("Kestrel server is offline.");
		}
	}
}
