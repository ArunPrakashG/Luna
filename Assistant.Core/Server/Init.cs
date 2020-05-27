using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;

namespace Assistant.Core.Server {
	internal sealed class Init {
		private readonly IConfiguration Configuration;
		private readonly IWebHostEnvironment WebhostEnvironment;

		public Init(IConfiguration _configuration, IWebHostEnvironment _hostingEnvironment) {
			Configuration = _configuration;
			WebhostEnvironment = _hostingEnvironment;
			Debug.WriteLine(WebhostEnvironment.ContentRootPath);
			Debug.WriteLine(WebhostEnvironment.WebRootPath);
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {			
			services.AddRazorPages().AddRazorRuntimeCompilation();
			services.AddSession();
			services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
			services.AddResponseCompression();
			IMvcBuilder mvc = services.AddControllersWithViews().AddRazorRuntimeCompilation();			
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

			app.UseForwardedHeaders();
			app.UseResponseCompression();
			app.UseWebSockets();
			app.UseSession();
			app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase), appBuilder => appBuilder.UseStatusCodePagesWithReExecute("/"));			
			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseEndpoints(endpoints => {
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Dashboard}/{action=Index}/{id?}");
			});
		}
	}
}
