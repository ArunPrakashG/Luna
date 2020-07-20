using Luna.Gpio;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Luna.Web {
	public class Program {
		private static int KestrelPort = 5335;
		private static IHost WebHost;
		private static GpioCore GpioCore;

		public static async Task Main(string[] args) {
			var pins = new AvailablePins(new int[10], new int[10], new int[10], new int[10], new int[10], new int[10]);
			GpioCore = new GpioCore(pins, false);
			WebHost = CreateHostBuilder().Build();
			await WebHost.StartAsync().ConfigureAwait(false);
			WebHost.WaitForShutdown();
		}

		internal static GpioCore GetGpioCore() => GpioCore;

		private static IHostBuilder CreateHostBuilder() {
			return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(
				(s) =>
				s.UseUrls($"http://*:{KestrelPort}", $"https://*:{KestrelPort + 1}")
				.UseKestrel()
				.ConfigureKestrel((k) => {					
					k.AddServerHeader = true;
					k.ConfigureEndpointDefaults((l) => {
						l.Protocols = HttpProtocols.Http1AndHttp2;
					});
				})
				.UseStartup<Startup>()
				.UseIIS()
				.UseIISIntegration());			
		}
	}
}
