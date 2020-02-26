using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Assistant.Rest {
	public class RestCore {
		private static int ServerPort = 7777;
		private static IHost? WebHost;

		public RestCore(int port) {
			ServerPort = port;
		}

		public RestCore() {
			ServerPort = 7777;
		}

		public async Task InitServer() {
			WebHost = CreateHostBuilder().Build();
			await WebHost.StartAsync().ConfigureAwait(false);			
		}

		public static IHost? GetHost() => WebHost;

		public async Task Shutdown() {
			if(WebHost == null) {
				return;
			}

			await WebHost.StopAsync().ConfigureAwait(false);
		}

		public static IHostBuilder CreateHostBuilder() {
			return Host.CreateDefaultBuilder()
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseKestrel(options => options.ListenAnyIP(ServerPort));
					webBuilder.UseStartup<Startup>();
				});
		}			
	}
}
