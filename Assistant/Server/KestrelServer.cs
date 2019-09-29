
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
using Assistant.Server.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Assistant.Server {
	public static class KestrelServer {
		private static IHost KestrelWebHost;
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
			Logger.Log($"Kestrel server is running at http://*:9090/");
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
