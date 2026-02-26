using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;
using System.Text.Encodings.Web;

namespace TearmaWeb.Rules;

public class RedirectToWwwRule(IWebHostEnvironment environment) : IRule
{
    public void ApplyRule(RewriteContext context)
    {
        context.Result = RuleResult.ContinueRules;

        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        var path = request.Path.Value ?? string.Empty;
        var domain = request.Host.Host;

        // Redirect to www
        if (environment.IsProduction()
            && domain != "www.tearma.ie"
            && domain != "super.tearma.ie"
            && domain != "test.tearma.ie")
        {
            var urlEncodedPath = EncodePath(path);
            response.StatusCode = 301;
            response.Headers[HeaderNames.Location] = $"https://www.tearma.ie{urlEncodedPath}";
            context.Result = RuleResult.EndResponse;
            return;
        }

        // Redirect to https
        if (environment.IsProduction() && !request.IsHttps)
        {
            var urlEncodedPath = EncodePath(path);
            response.StatusCode = 301;

            if (domain == "www.tearma.ie")
                response.Headers[HeaderNames.Location] = $"https://www.tearma.ie{urlEncodedPath}";
            else if (domain == "test.tearma.ie")
                response.Headers[HeaderNames.Location] = $"https://test.tearma.ie{urlEncodedPath}";
            else if (domain == "super.tearma.ie")
                response.Headers[HeaderNames.Location] = $"https://super.tearma.ie{urlEncodedPath}";

            context.Result = RuleResult.EndResponse;
            return;
        }

        // Redirect old simple URLs
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

        if (urls.TryGetValue(path, out var redirect))
        {
            response.StatusCode = 301;
            response.Headers[HeaderNames.Location] = redirect;
            context.Result = RuleResult.EndResponse;
            return;
        }

        // Redirect old quick search URL
        var query = request.Query;

        if (path.Equals("/search.aspx", StringComparison.OrdinalIgnoreCase)
            && query.TryGetValue("term", out var termValues))
        {
            var termRaw = termValues.ToString();
            var term = Models.Home.Tools.SlashEncode(termRaw);
            term = UrlEncoder.Default.Encode(term);

            response.StatusCode = 301;
            response.Headers[HeaderNames.Location] = $"/q/{term}/";
            context.Result = RuleResult.EndResponse;
        }
    }

    private static string EncodePath(string path)
    {
        // Encode each segment individually
        return string
            .Join("/", path
                .Split("/", StringSplitOptions.RemoveEmptyEntries)
                .Select(s => UrlEncoder.Default.Encode(s)))
            .Insert(0, path.StartsWith("/") ? "/" : "");
    }
}
