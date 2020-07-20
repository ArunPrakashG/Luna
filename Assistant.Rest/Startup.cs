using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Luna.Rest {
	public class Startup {
		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
			services.AddResponseCompression();
			services.AddCors(builder => builder.AddDefaultPolicy(policyBuilder => policyBuilder.AllowAnyOrigin()));
			services.AddLogging(builder => {
				builder.AddFilter("Microsoft", LogLevel.Warning)
			   .AddFilter("System", LogLevel.Warning)
			   .AddFilter("NToastNotify", LogLevel.Warning)
			   .AddConsole();
			});
			IMvcBuilder mvc = services.AddControllers();
			mvc.SetCompatibilityVersion(CompatibilityVersion.Latest);
			mvc.AddNewtonsoftJson(
				options => {
					options.SerializerSettings.ContractResolver = new DefaultContractResolver();
					options.SerializerSettings.Formatting = Formatting.Indented;
				}
			);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();
			app.UseForwardedHeaders();
			app.UseResponseCompression();
			app.UseWebSockets();
			app.UseRouting();
			app.UseEndpoints(endpoints => endpoints.MapControllers());
		}
	}
}
