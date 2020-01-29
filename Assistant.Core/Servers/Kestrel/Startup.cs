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

namespace Assistant.Servers.Kestrel {
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
