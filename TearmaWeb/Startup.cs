using BotDetect.Web;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using TearmaWeb.Rules;

namespace TearmaWeb;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); // needed by Captcha

        // MVC
        services.AddRouting(options =>
        {
            options.AppendTrailingSlash = true;
        });

        services.AddControllersWithViews();

        // Session (needed by Captcha)
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.IsEssential = true;
        });

        // MiniProfiler
        services.AddMiniProfiler();

        // Exceptional
        services.AddExceptional(configuration.GetSection("Exceptional"), settings =>
        {
            settings.Ignore.Types =
            [
                "System.InvalidOperationException",
                "Microsoft.AspNetCore.Connections.ConnectionResetException"
            ];
        });

        // WebOptimizer
        services.AddWebOptimizer();

        // QueryLogger
        services.AddQueryLogger(configuration.GetSection("QueryLogger"));

        services.AddScoped<Controllers.Broker>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Important so that Prettify knows where to look for sound files:
        Controllers.Prettify.ContentPath = env.ContentRootPath;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseStatusCodePages();
        }
        else
        {
            app.UseExceptionHandler("/error/");
            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseExceptional();
        }

        // Session must come before Captcha
        app.UseSession();

        // Captcha middleware (must be after session)
        app.UseCaptcha(configuration);

        // Rewrite rules
        var options = new RewriteOptions();
        options.Rules.Add(new RedirectToWwwRule(environment));
        app.UseRewriter(options);

        if (env.IsProduction())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseMiniProfiler();
        app.UseWebOptimizer();
        app.UseStaticFiles();
        app.UseRouting();

        // Endpoint routing (replacement for UseMvc)
        app.UseEndpoints(endpoints =>
        {
            // Home page
            endpoints.MapControllerRoute(
                name: "home",
                pattern: "/",
                defaults: new { controller = "Home", action = "Index" });

            // Error page
            endpoints.MapControllerRoute(
                name: "error",
                pattern: "/error/{code:int?}",
                defaults: new { controller = "Home", action = "Error" });

            // A single entry
            endpoints.MapControllerRoute(
                name: "entry",
                pattern: "/id/{id:int}/",
                defaults: new { controller = "Home", action = "Entry" });

            // Quick search
            endpoints.MapControllerRoute(
                name: "quicksearch",
                pattern: "/q/{word}/{lang?}",
                defaults: new { controller = "Home", action = "QuickSearch" });

            // Advanced search
            endpoints.MapControllerRoute(
                name: "advsearch",
                pattern: "/plus/",
                defaults: new { controller = "Home", action = "AdvSearch" });

            endpoints.MapControllerRoute(
                name: "advsearch2",
                pattern: "/plus/{word}/{length:regex(^(al|sw|mw)$)}/{extent:regex(^(al|st|ed|pt|md|ft)$)}/lang{lang}/pos{posLabel:int}/dom{domainID:int}/{page:int?}",
                defaults: new { controller = "Home", action = "AdvSearch" });

            // Browse by domain
            endpoints.MapControllerRoute(
                name: "domains",
                pattern: "/dom/{lang:regex(^(ga|en)$)}/",
                defaults: new { controller = "Home", action = "Domains" });

            endpoints.MapControllerRoute(
                name: "domain",
                pattern: "/dom/{domID:int}/{lang:regex(^(ga|en)$)}/{page:int?}",
                defaults: new { controller = "Home", action = "Domain" });

            // Info
            endpoints.MapControllerRoute(
                name: "info-topic",
                pattern: "/eolas/{nickname}.{lang}",
                defaults: new { controller = "Info", action = "Topic", section = "eolas" });

            endpoints.MapControllerRoute(
                name: "info-lang",
                pattern: "/eolas/{lang?}",
                defaults: new { controller = "Info", action = "Topic", section = "eolas" });

            // Cabhair
            endpoints.MapControllerRoute(
                name: "cabhair-topic",
                pattern: "/cabhair/{nickname}.{lang}",
                defaults: new { controller = "Info", action = "Topic", section = "cabhair" });

            endpoints.MapControllerRoute(
                name: "cabhair-lang",
                pattern: "/cabhair/{lang?}",
                defaults: new { controller = "Info", action = "Topic", section = "cabhair" });

            // Download
            endpoints.MapControllerRoute(
                name: "download",
                pattern: "/ioslodail/",
                defaults: new { controller = "Info", action = "Download" });

            // Widgets
            endpoints.MapControllerRoute(
                name: "widgets",
                pattern: "/breiseain/",
                defaults: new { controller = "Info", action = "Widgets" });

            endpoints.MapControllerRoute(
                name: "widgets-box",
                pattern: "/breiseain/bosca/",
                defaults: new { controller = "Widgets", action = "Box" });

            endpoints.MapControllerRoute(
                name: "widgets-tod",
                pattern: "/breiseain/tearma-an-lae/",
                defaults: new { controller = "Widgets", action = "Tod" });

            // Ask
            endpoints.MapControllerRoute(
                name: "ask",
                pattern: "/ceist/",
                defaults: new { controller = "Ask", action = "Ask" });
        });
    }
}
