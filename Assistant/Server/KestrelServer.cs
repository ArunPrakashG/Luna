using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.Server {

	public static class KestrelServer {
		private static IWebHost KestrelWebHost;
		private static readonly Logger Logger = new Logger("KESTREL-SERVER");
		public static bool IsServerOnline;

		public static async Task Start() {
			if (KestrelWebHost != null) {
				return;
			}

			Logger.Log("Starting Kestrel IPC server...", Enums.LogLevels.Trace);
			IWebHostBuilder builder = new WebHostBuilder();
			string absoluteConfigDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.ConfigDirectory);
			builder.ConfigureLogging(logging => logging.SetMinimumLevel(Core.Config.Debug ? LogLevel.Trace : LogLevel.Warning));
			if (File.Exists(Path.Combine(absoluteConfigDirectory, Constants.KestrelConfigurationFile))) {
				builder.UseConfiguration(new ConfigurationBuilder().SetBasePath(absoluteConfigDirectory).AddJsonFile(Constants.KestrelConfigurationFile, false, true).Build());
				builder.UseKestrel((builderContext, options) => options.Configure(builderContext.Configuration.GetSection("Kestrel")));
				if (Helpers.IsNullOrEmpty(Core.Config.KestrelServerUrl)) {
					builder.UseUrls("http://localhost:9090");
				}
				else {
					builder.UseUrls(Core.Config.KestrelServerUrl);
				}
				builder.ConfigureLogging((hostingContext, logging) => logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging")));
			}
			else {
				builder.UseKestrel();
				if (Helpers.IsNullOrEmpty(Core.Config.KestrelServerUrl)) {
					builder.UseUrls("http://localhost:9090/");
				}
				else {
					builder.UseUrls(Core.Config.KestrelServerUrl);
				}
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
			if (Helpers.IsNullOrEmpty(Core.Config.KestrelServerUrl)) {
				Logger.Log($"Kestrel server is running at http://localhost:9090/");
			}
			else {
				Logger.Log($"Kestrel server is running at {Core.Config.KestrelServerUrl}");
			}
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
