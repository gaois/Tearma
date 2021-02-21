using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;

namespace TearmaWeb.Rules
{
    public class RedirectToWwwRule : IRule {
        private readonly IHostingEnvironment _environment;

        public RedirectToWwwRule(IHostingEnvironment environment) {
            _environment = environment;
        }

        public void ApplyRule(RewriteContext context) {
			context.Result = RuleResult.ContinueRules;
			var request = context.HttpContext.Request;
            var path = request.Path.Value;
            var response = context.HttpContext.Response;

            //Redirect to www:
            var domain = request.Host.Host;

			if (_environment.IsProduction()
                && domain != "www.tearma.ie" && domain != "super.tearma.ie" && domain != "test.tearma.ie") {
                var urlEncodedPath = string.Join("/", path.Split("/").Select(s => UrlEncoder.Default.Encode(s)));
				response.StatusCode = 301;
				response.Headers[HeaderNames.Location] = string.Concat("https://www.tearma.ie", urlEncodedPath);
				context.Result = RuleResult.EndResponse;
			}

            //Redirect to https:
            if (_environment.IsProduction() && !request.IsHttps) {
                var urlEncodedPath = string.Join("/", path.Split("/").Select(s => UrlEncoder.Default.Encode(s)));
                response.StatusCode = 301;
				if(domain == "www.tearma.ie") response.Headers[HeaderNames.Location] = string.Concat("https://www.tearma.ie", urlEncodedPath);
                if (domain == "test.tearma.ie") response.Headers[HeaderNames.Location] = string.Concat("https://test.tearma.ie", urlEncodedPath);
                if (domain == "super.tearma.ie") response.Headers[HeaderNames.Location] = string.Concat("https://super.tearma.ie", urlEncodedPath);
                context.Result = RuleResult.EndResponse;
            }

            //Redirect (simple) old URLs:
            var urls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "/home.aspx", "/" },
                { "/searchbox.aspx", "/breiseain/bosca/" },
                { "/tal.aspx", "/breiseain/tearma-an-lae/" },
                { "/liostai.aspx", "/ioslodail/" },
                { "/widgets.aspx", "/breiseain/" },
                { "/enquiry.aspx", "/ceist/" },
                { "/help.aspx", "/cabhair/" },
                { "/about.aspx", "/eolas/" }
            };

			if (urls.ContainsKey(path)) {
				response.StatusCode = 301;
				response.Headers[HeaderNames.Location] = urls[path];
				context.Result = RuleResult.EndResponse;
			}

            //redirect old quick search URL:
            var query = request.Query;

            if (path.Equals("/search.aspx", StringComparison.OrdinalIgnoreCase) && query.ContainsKey("term")) {
				response.StatusCode = 301;
				var x = Models.Home.Tools.SlashEncode(query["term"]);
				x = UrlEncoder.Default.Encode(x);
				response.Headers[HeaderNames.Location] = $"/q/{x}/";
				context.Result = RuleResult.EndResponse;
			}
		}
	}
}