using Ansa.Extensions;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers;

public partial class HomeController(IQueryLogger queryLogger, Broker broker) : Controller
{
    [GeneratedRegex(@"^\#[0-9]+$")]
    private static partial Regex HashIdRegex();

    private static bool IsSuper(HttpRequest request) =>
        request.Host.Host.Equals("super.tearma.ie", StringComparison.OrdinalIgnoreCase);

    private static string DecodeSlashes(string text)
    {
        return text
            .Replace("$backslash;", "\\")
            .Replace("$forwardslash;", "/");
    }

    // ---------------------------
    // Home page
    // ---------------------------
    [OutputCache]
    public async Task<IActionResult> Index()
    {
        var model = new Models.Home.Index();
        await broker.DoIndexAsync(model);

        ViewData["PageTitle"] = "téarma.ie";
        ViewData["TagLine"] = "An Bunachar Náisiúnta Téarmaíochta don Ghaeilge · The National Terminology Database for Irish";
        ViewData["IsSuper"] = IsSuper(Request);

        return View("Index", model);
    }

    // ---------------------------
    // Single entry page
    // ---------------------------
    public async Task<IActionResult> Entry(int id)
    {
        var model = new Entry { Id = id };
        await broker.DoEntryAsync(model);

        ViewData["PageTitle"] = "téarma.ie";
        ViewData["TagLine"] = "An Bunachar Náisiúnta Téarmaíochta don Ghaeilge · The National Terminology Database for Irish";
        ViewData["IsSuper"] = IsSuper(Request);

        return View("Entry", model);
    }

    // ---------------------------
    // Quick search
    // ---------------------------
    [OutputCache]
    public async Task<IActionResult> QuickSearch(string word, string? lang)
    {
        if (word.IsNullOrWhiteSpace())
            return RedirectToAction("Index");

        // #123 → /id/123
        if (HashIdRegex().IsMatch(word))
            return Redirect("/id/" + word.Replace("#", ""));

        using var stopwatch = new SimpleTimer();

        var model = new QuickSearch
        {
            Word = DecodeSlashes(word),
            Lang = lang ?? ""
        };

        if (IsSuper(Request))
            model.Super = true;

        await broker.DoQuickSearchAsync(model);

        var query = new Query
        {
            QueryCategory = "QuickSearch",
            QueryTerms = word,
            QueryText = Request.Path,
            ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
            ResultCount = model.Exacts.Count,
            JsonData = model.SearchData()
        };

        queryLogger.Log(query);

        ViewData["PageTitle"] = $"\"{model.Word}\"";
        ViewData["IsSuper"] = IsSuper(Request);

        // Plausible analytics
        ViewData["IsTextSearch"] = "true";
        ViewData["IsTextSearchResultful"] =
            (model.Exacts.Count > 0 || model.Relateds.Count > 0) ? "true" : "false";
        ViewData["SearchText00"] =
            (model.Exacts.Count == 0 && model.Relateds.Count == 0) ? model.Word : "";
        ViewData["SearchText01"] =
            (model.Exacts.Count == 0 && model.Relateds.Count > 0) ? model.Word : "";
        ViewData["SearchText1X"] =
            (model.Exacts.Count > 0) ? model.Word : "";

        return View("QuickSearch", model);
    }

    // ---------------------------
    // Advanced search
    // ---------------------------
    [OutputCache]
    public async Task<IActionResult> AdvSearch(
        string? word,
        string length,
        string extent,
        string? lang,
        int posLabel,
        int domainID,
        int page)
    {
        using var stopwatch = new SimpleTimer();

        lang ??= "";
        if (page < 1) page = 1;

        var model = new AdvSearch
        {
            Word = DecodeSlashes(word ?? ""),
            Length = length,
            Extent = extent,
            Lang = lang != "0" ? lang : "",
            PosLabel = posLabel,
            DomainID = domainID,
            Page = page
        };

        if (model.Word.IsNullOrWhiteSpace())
        {
            await broker.PrepareAdvSearchAsync(model);
            ViewData["PageTitle"] = "Cuardach casta · Advanced search";
        }
        else
        {
            await broker.DoAdvSearchAsync(model);

            var query = new Query
            {
                QueryCategory = "AdvSearch",
                QueryTerms = word,
                QueryText = Request.Path,
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                ResultCount = model.Matches.Count,
                JsonData = model.SearchData()
            };

            queryLogger.Log(query);

            ViewData["PageTitle"] = $"\"{model.Word}\" | Cuardach casta · Advanced search";

            // Plausible analytics
            ViewData["IsTextSearch"] = "true";
            ViewData["IsTextSearchResultful"] =
                (model.Matches.Count > 0) ? "true" : "false";
        }

        ViewData["IsSuper"] = IsSuper(Request);

        return View("AdvSearch", model);
    }

    // ---------------------------
    // Domain list
    // ---------------------------
    [OutputCache]
    public async Task<IActionResult> Domains(string? lang)
    {
        var model = new Domains
        {
            Lang = lang ?? ""
        };

        await broker.DoDomainsAsync(model);

        ViewData["PageTitle"] = "Brabhsáil · Browse";
        ViewData["IsSuper"] = IsSuper(Request);

        return View("Domains", model);
    }

    // ---------------------------
    // Single domain page
    // ---------------------------
    public async Task<IActionResult> Domain(int domID, string? lang, int page = 1)
    {
        using var stopwatch = new SimpleTimer();

        var model = new Domain
        {
            Lang = lang ?? "",
            DomID = domID,
            Page = page
        };

        await broker.DoDomainAsync(model);

        var query = new Query
        {
            QueryCategory = "Domain",
            QueryTerms = domID.ToString(),
            QueryText = Request.Path,
            ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
            ResultCount = model.Matches.Count,
            JsonData = model.SearchData()
        };

        queryLogger.Log(query);

        if (model.DomainListing is null)
            return NotFound();

        ViewData["PageTitle"] = "Brabhsáil · Browse";
        ViewData["IsSuper"] = IsSuper(Request);

        return View("Domain", model);
    }

    // ---------------------------
    // Error page
    // ---------------------------
    public IActionResult Error(int? code)
    {
        var model = new Models.ErrorModel
        {
            HttpStatusCode = code ?? HttpContext.Response.StatusCode,
            RequestID = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        switch (model.HttpStatusCode)
        {
            case 404:
                ViewData["PageTitle"] = "Earráid 404 · Error 404";
                ViewData["MetaDescription"] = "Níor aimsíodh an leathanach · Page not found";
                break;

            default:
                ViewData["PageTitle"] = "Earráid · Error";
                ViewData["MetaDescription"] =
                    "Tharla earráid agus an leathanach seo á oscailt · An error occurred while attempting to open this page";
                break;
        }

        ViewData["IsSuper"] = IsSuper(Request);

        return View(model);
    }
}
