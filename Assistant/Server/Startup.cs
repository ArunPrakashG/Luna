using System;
using System.Collections.Generic;
using System.Text;
using HomeAssistant.AssistantCore;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace HomeAssistant.Server {
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
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo {
					Version = "v1",
					Title = "Home assistant API",
					Description = "Documentation for tess api endpoints.",
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
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "TESS api documentation");
				c.RoutePrefix = string.Empty;
			});
			app.UseMvc();
		}
	}
}
