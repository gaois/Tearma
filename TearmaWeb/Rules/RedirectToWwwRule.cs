using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using System.Collections.Generic;
using System.Net;
using System.Text.Encodings.Web;

namespace TearmaWeb.Rules {
    public class RedirectToWwwRule : IRule {
		public virtual void ApplyRule(RewriteContext context) {
			context.Result=RuleResult.ContinueRules;
			HttpRequest req = context.HttpContext.Request;
            HttpResponse response = context.HttpContext.Response;

            //Redirect to www:
            string domain=req.Host.Host;
			if(domain!="localhost" && domain!="www-tearma-ie.gaois.ie" && domain!="www.tearma.ie") {
				response.StatusCode=301;
				response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location]="https://www.tearma.ie" + req.Path.Value;
				context.Result=RuleResult.EndResponse;
			}

            //Redirect to https:
            if(domain != "localhost" && domain != "www-tearma-ie.gaois.ie" && !req.IsHttps) {
                var requestPath = WebUtility.UrlEncode(req.Path.Value);
                response.StatusCode = 301;
                response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location] = "https://www.tearma.ie" + requestPath;
                context.Result = RuleResult.EndResponse;
            }

            //Redirect (simple) old URLs:
            Dictionary<string, string> urls = new Dictionary<string, string>
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
            string path=req.Path.Value.ToLower();
			if(urls.ContainsKey(path)) {
				response.StatusCode=301;
				response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location]=urls[path];
				context.Result=RuleResult.EndResponse;
			}

			//redirect old quick search URL:
			if(req.Path.Value.ToLower()=="/search.aspx" && req.Query.ContainsKey("term")) {
				response.StatusCode=301;
				string x=Models.Home.Tools.SlashEncode(req.Query["term"]);
				x=UrlEncoder.Default.Encode(x);
				response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location]="/q/"+x+"/";
				context.Result=RuleResult.EndResponse;
			}
		}
	}
}