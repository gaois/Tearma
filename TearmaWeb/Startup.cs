using Gaois.QueryLogger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Rewrite;
using System;
using TearmaWeb.Rules;
using BotDetect.Web;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

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
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); //needed by Captcha

			services.AddMvc();

            // Add Session services. Needed by Captcha.
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.IsEssential = true;
            });

            services.AddMiniProfiler();
            services.AddExceptional(_configuration.GetSection("Exceptional"), settings => {
                settings.Ignore.Types = new HashSet<string>() {
                    "System.InvalidOperationException",
                    "Microsoft.AspNetCore.Connections.ConnectionResetException"
                };
            });
            services.AddWebOptimizer(pipeline =>
            {
                pipeline.MinifyCssFiles("furniture/**/*.css");
                pipeline.MinifyJsFiles("furniture/**/*.js");
            });

            services.AddQueryLogger(_configuration.GetSection("QueryLogger"));

            services.AddScoped<Controllers.Broker>();
            services.AddScoped<Controllers.IateBroker>();
        }

		public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env) {

            //important so that Prettify knows where to look for sound files:
            Controllers.Prettify.ContentPath=env.ContentRootPath;

            if (_environment.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseStatusCodePages();
            } else {
                app.UseExceptionHandler("/error/");
                app.UseStatusCodePagesWithReExecute("/error/{0}");
                app.UseExceptional();
            }

            // configures Session middleware
            app.UseSession();
            // configure your application pipeline to use Captcha middleware
            // Important! UseCaptcha(...) must be called after the UseSession() call
            app.UseCaptcha(_configuration);

            var options=new RewriteOptions();
			options.Rules.Add(new RedirectToWwwRule(_environment));
			app.UseRewriter(options);

            if (_environment.IsProduction()) {
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
                routes.MapRoute(name: "", template: "/error/{code:int?}", defaults: new { controller = "Home", action = "Error" });

                //A single entry:
                routes.MapRoute(name: "", template: "/id/{id:int}/", defaults: new {controller="Home", action="Entry"});

				//Quick search:
				routes.MapRoute(name: "", template: "/q/{word}/{lang?}/", defaults: new {controller="Home", action="QuickSearch", lang=""});
                routes.MapRoute(name: "", template: "/peekTearma.json/", defaults: new { controller = "Peek", action = "PeekTearma" });

                //Iate search:
                routes.MapRoute(name: "", template: "/iate/{word}/{lang?}/", defaults: new { controller = "Iate", action = "Search", lang = "" });
                routes.MapRoute(name: "", template: "/peekIate.json/", defaults: new { controller = "Peek", action = "PeekIate" });

                //Advanced search:
                routes.MapRoute(name: "", template: "/plus/", defaults: new {controller="Home", action="AdvSearch"});
				routes.MapRoute(name: "", template: "/plus/{word}/{length:regex(^(al|sw|mw)$)}/{extent:regex(^(al|st|ed|pt|md|ft)$)}/lang{lang}/pos{posLabel:int}/dom{domainID:int}/{page:int?}/", defaults: new {controller="Home", action="AdvSearch", page=1});

				//Browse by domain:
				routes.MapRoute(name: "", template: "/dom/{lang:regex(^(ga|en)$)}/", defaults: new {controller="Home", action="Domains"});
				routes.MapRoute(name: "", template: "/dom/{domID:int}/{lang:regex(^(ga|en)$)}/{page:int?}/", defaults: new {controller="Home", action="Domain", page=1});

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