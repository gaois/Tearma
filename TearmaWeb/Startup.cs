using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Rewrite;
using Gaois.QueryLogger;

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

            services.AddQueryLogger(settings =>
            {
                settings.ApplicationName = "Téarma";
                settings.IsEnabled = _environment.IsProduction();
                settings.Store.ConnectionString = _configuration.GetConnectionString("query_logger");
            });
        }

		public class RedirectToWwwRule : IRule {
			public virtual void ApplyRule(RewriteContext context) {
				context.Result=RuleResult.ContinueRules;
				HttpRequest req = context.HttpContext.Request;

				//Redirect to www:
				string domain=req.Host.Host;
				if(domain!="localhost" && domain!="eag-tearma-ie.gaois.ie" && domain!="www.tearma.ie") {
					HttpResponse response=context.HttpContext.Response;
					response.StatusCode=301;
					response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location]="http://www.tearma.ie"+req.Path.Value;
					context.Result=RuleResult.EndResponse;
				}

				//Redirect (simple) old URLs:
				Dictionary<string, string> urls=new Dictionary<string, string>();
				urls.Add("/home.aspx", "/");
				urls.Add("/searchbox.aspx", "/breiseain/bosca/");
				urls.Add("/tal.aspx", "/breiseain/tearma-an-lae/");
				urls.Add("/liostai.aspx", "/ioslodail/");
				urls.Add("/widgets.aspx", "/breiseain/");
				urls.Add("/enquiry.aspx", "/ceist/");
				urls.Add("/help.aspx", "/cabhair/");
				urls.Add("/about.aspx", "/eolas/");
				string path=req.Path.Value.ToLower();
				if(urls.ContainsKey(path)) {
					HttpResponse response=context.HttpContext.Response;
					response.StatusCode=301;
					response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location]=urls[path];
					context.Result=RuleResult.EndResponse;
				}

				//redirect old quick search URL:
				if(req.Path.Value.ToLower()=="/search.aspx" && req.Query.ContainsKey("term")) {
					HttpResponse response=context.HttpContext.Response;
					response.StatusCode=301;
					response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location]="/q/"+req.Query["term"]+"/";
					context.Result=RuleResult.EndResponse;
				}
			}
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if(env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseStatusCodePages();
			}

			RewriteOptions options=new RewriteOptions();
			options.Rules.Add(new RedirectToWwwRule());
			app.UseRewriter(options);

			app.UseStaticFiles();

			app.UseMvc(routes => {
				//Home page:
				routes.MapRoute(name: "", template: "/", defaults: new {controller="Home", action="Index"});

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