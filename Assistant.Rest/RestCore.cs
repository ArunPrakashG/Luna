using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Luna.Rest {
	public class RestCore {
		private static int ServerPort = 7777;
		private static IHost? WebHost;
		internal static Dictionary<string, Func<RequestParameter, RequestResponse>> RequestFuncs = new Dictionary<string, Func<RequestParameter, RequestResponse>>();

		public RestCore(int port) {
			ServerPort = port;
		}

		public RestCore() {
			ServerPort = 7777;
		}

		public async Task InitServer(Dictionary<string, Func<RequestParameter, RequestResponse>> requestFuncs) {
			RequestFuncs = requestFuncs;
			WebHost = CreateHostBuilder().Build();
			await WebHost.StartAsync().ConfigureAwait(false);			
		}

		internal static RequestResponse GetResponse(string? command, RequestParameter req) {
			if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(req.AuthToken) || string.IsNullOrEmpty(req.LocalIp) || string.IsNullOrEmpty(req.PublicIp)) {
				return default;
			}

			Func<RequestParameter, RequestResponse>? func = GetRequestFunc(command);

			if(func == null) {
				return default;
			}

			return func.Invoke(req);
		}

		private static Func<RequestParameter, RequestResponse>? GetRequestFunc(string? command) {
			if (string.IsNullOrEmpty(command) || RequestFuncs.Count <= 0) {
				return null;
			}

			if (RequestFuncs.ContainsKey(command)) {
				return RequestFuncs.GetValueOrDefault(command);
			}

			return null;
		}

		public static IHost? GetHost() => WebHost;

		public async Task Shutdown() {
			if(WebHost == null) {
				return;
			}

			await WebHost.StopAsync().ConfigureAwait(false);
			WebHost.Dispose();			
		}

		private static IHostBuilder CreateHostBuilder() {
			return Host.CreateDefaultBuilder()
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseKestrel(options => options.ListenAnyIP(ServerPort));
					webBuilder.UseStartup<Startup>();
				});
		}

		public enum GpioPinState {
			On = 0,
			Off = 1
		}

		public enum GpioPinMode {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}
	}
}
