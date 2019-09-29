
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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Assistant.Server {
	public class Startup {
		private readonly IConfiguration Configuration;

		public Startup([NotNull] IConfiguration configuration) => Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if ((app == null) || (env == null)) {
				return;
			}

			app.UseForwardedHeaders();
			app.UseResponseCompression();
			app.UseWebSockets();
			app.UseRouting();
			app.UseEndpoints(endpoints => endpoints.MapControllers());
			app.UseSwagger();
			app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/HomeAssistant/swagger.json", "HomeAssistant API"));
		}



		public void ConfigureServices(IServiceCollection services) {
			if (services == null) {
				return;
			}

			services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
			services.AddResponseCompression();
			services.AddCors(builder => builder.AddDefaultPolicy(policyBuilder => policyBuilder.AllowAnyOrigin()));
			services.AddSwaggerGen(
				options => {
					options.EnableAnnotations();
					options.SwaggerDoc(
						"HomeAssistant", new OpenApiInfo {
							License = new OpenApiLicense {
								Name = "MIT"
							},

							Title = "HomeAssistant API"
						}
					);
				}
			);
			IMvcBuilder mvc = services.AddControllers();
			mvc.SetCompatibilityVersion(CompatibilityVersion.Latest);
			mvc.AddNewtonsoftJson(
				options => {
					options.SerializerSettings.ContractResolver = new DefaultContractResolver();
					options.SerializerSettings.Formatting = Formatting.Indented;
				}
			);
		}
	}
}
