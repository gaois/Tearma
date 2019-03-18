using Gaois.QueryLogger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Rewrite;
using System;
using TearmaWeb.Rules;

namespace TearmaWeb
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;

        public Startup(IConfiguration configuration, IHostingEnvironment environment) {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services) {
			services.AddMvc();

            services.AddMiniProfiler();
            services.AddExceptional(_configuration.GetSection("Exceptional"));
            services.AddWebOptimizer();

            services.AddQueryLogger(settings =>
            {
                settings.ApplicationName = "Téarma";
                settings.IsEnabled = _environment.IsProduction();
				settings.Store.ConnectionString = _configuration.GetConnectionString("Users");
            });

            services.AddScoped<Controllers.Broker>();
        }

		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseStatusCodePages();
            } else {
                app.UseExceptionHandler("/error/500");
                app.UseStatusCodePagesWithReExecute("/error/{0}");
                app.UseExceptional();
            }

            var options=new RewriteOptions();
			options.Rules.Add(new RedirectToWwwRule());
			app.UseRewriter(options);

            if (env.IsProduction()) {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseMiniProfiler();
            app.UseWebOptimizer();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.Add("Cache-Control", "public,max-age=2678400");
                    context.Context.Response.Headers.Add("Expires", DateTime.UtcNow.AddDays(31).ToString("R"));
                }
            });

			app.UseMvc(routes => {
				//Home page:
				routes.MapRoute(name: "", template: "/", defaults: new {controller="Home", action="Index"});

                //Error page:
                routes.MapRoute(name: "", template: "/error/{code:int}", defaults: new { controller = "Home", action = "Error" });

                //A single entry:
                routes.MapRoute(name: "", template: "/id/{id:int}/", defaults: new {controller="Home", action="Entry"});

				//Quick search:
				routes.MapRoute(name: "", template: "/q/{word}/{lang?}/", defaults: new {controller="Home", action="QuickSearch", lang=""});

				//Advanced search:
				routes.MapRoute(name: "", template: "/plus/", defaults: new {controller="Home", action="AdvSearch"});
				routes.MapRoute(name: "", template: "/plus/{word}/{length:regex(^(al|sw|mw)$)}/{extent:regex(^(al|st|ed|pt|md|ft)$)}/lang{lang}/pos{posLabel:int}/dom{domainID:int}/sub{subdomainID:int}/{page:int?}/", defaults: new {controller="Home", action="AdvSearch", page=1});

				//Browse by domain:
				routes.MapRoute(name: "", template: "/dom/{lang:regex(^(ga|en)$)}/", defaults: new {controller="Home", action="Domains"});
				routes.MapRoute(name: "", template: "/dom/{domID:int}/{lang:regex(^(ga|en)$)}/{page:int?}/", defaults: new {controller="Home", action="Domain", page=1});
				routes.MapRoute(name: "", template: "/dom/{domID:int}/{subdomID:int}/{lang:regex(^(ga|en)$)}/{page:int?}/", defaults: new {controller="Home", action="Domain", page=1});

				//Info:
				routes.MapRoute(name: "", template: "/eolas/{nickname}.{lang}", defaults: new {controller="Info", action="Topic", section="eolas"});
				routes.MapRoute(name: "", template: "/eolas/{lang?}/", defaults: new {controller="Info", action="Topic", section="eolas", lang="ga"});

				//cabhair:
				routes.MapRoute(name: "", template: "/cabhair/{nickname}.{lang}", defaults: new {controller="Info", action="Topic", section="cabhair"});
				routes.MapRoute(name: "", template: "/cabhair/{lang?}/", defaults: new {controller="Info", action="Topic", section="cabhair", lang="ga"});

				//Download:
				routes.MapRoute(name: "", template: "/ioslodail/", defaults: new {controller="Info", action="Download"});

				//Widgets:
				routes.MapRoute(name: "", template: "/breiseain/", defaults: new {controller="Info", action="Widgets"});
				routes.MapRoute(name: "", template: "/breiseain/bosca/", defaults: new {controller="Widgets", action="Box"});
				routes.MapRoute(name: "", template: "/breiseain/tearma-an-lae/", defaults: new {controller="Widgets", action="Tod"});

				//Ask:
				routes.MapRoute(name: "", template: "/ceist/", defaults: new {controller="Ask", action="Ask"});
			});
		}
	}
}