using System;
using System.Collections.Generic;
using System.Text;
using HomeAssistant.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
			app.UseResponseCompression();
			app.UseWebSockets();
			app.UseMvc();
		}
	}
}
