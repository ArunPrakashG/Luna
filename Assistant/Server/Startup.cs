
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
using Assistant.Server.SignalR.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Assistant.Server {
	public class Startup {

		public void ConfigureServices(IServiceCollection services) {
			services.AddResponseCompression();
			services.AddCors(builder => builder.AddDefaultPolicy(policyBuilder => policyBuilder.AllowAnyOrigin()));
			IMvcCoreBuilder mvc = services.AddMvcCore();
			mvc.AddApiExplorer();
			mvc.SetCompatibilityVersion(CompatibilityVersion.Latest);
			mvc.AddFormatterMappings();
			mvc.AddJsonFormatters();			
			mvc.AddJsonOptions(
				options => {
					options.SerializerSettings.ContractResolver = new DefaultContractResolver();
					options.SerializerSettings.Formatting = Formatting.Indented;
				}
			);

			services.AddSignalR();
			services.AddSwaggerGen(c => {
				c.SwaggerDoc("v1", new OpenApiInfo {
					Version = "v1",
					Title = "Home assistant API",
					Description = $"Documentation for {Core.AssistantName} api endpoints.",
					Contact = new OpenApiContact() {
						Name = "Arun Prakash",
						Email = "arun.prakash.456789@gmail.com",
						Url = new Uri("https://github.com/SynergYFTW/HomeAssistant")
					},
					License = new OpenApiLicense() {
						Name = "MIT License",
						Url = new Uri("https://github.com/SynergYFTW/HomeAssistant/blob/master/LICENSE")
					}
				});
			});
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
			app.UseSwagger();
			app.UseResponseCompression();
			app.UseWebSockets();
			app.UseSignalR(routes => {
				routes.MapHub<ChatHub>("/chat");
			});
			app.UseSwaggerUI(c => {
				c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Core.AssistantName} api documentation");
				c.RoutePrefix = string.Empty;
			});
			app.UseMvc();
		}
	}
}
