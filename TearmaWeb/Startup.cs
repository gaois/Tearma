using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TearmaWeb {
	public class Startup {
		public void ConfigureServices(IServiceCollection services) {
			services.AddMvc();
		}
		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if(env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseStatusCodePages();
			}
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
				routes.MapRoute(name: "", template: "/info/{nickname}.{lang}", defaults: new {controller="Info", action="Topic", section="info"});
				routes.MapRoute(name: "", template: "/info/{lang?}/", defaults: new {controller="Info", action="Topic", section="info", lang="ga"});

				//Help:
				routes.MapRoute(name: "", template: "/help/{nickname}.{lang}", defaults: new {controller="Info", action="Topic", section="help"});
				routes.MapRoute(name: "", template: "/help/{lang?}/", defaults: new {controller="Info", action="Topic", section="help", lang="ga"});

				//Download:
				routes.MapRoute(name: "", template: "/download/", defaults: new {controller="Info", action="Download"});
			});
		}
	}
}
