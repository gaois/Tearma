using Ansa.Extensions;
using Microsoft.AspNetCore.Mvc;
using TearmaWeb.Models.Iate;

namespace TearmaWeb.Controllers;

public class IateController(IateBroker iateBroker) : Controller
{
    private static bool IsSuper(HttpRequest request)
    {
        //return request.Host.Host=="localhost";
        return request.Host.Host == "super.tearma.ie";
    }

    private static string MyDecodeShashes(string text)
    {
        text = text.Replace("$backslash;", @"\");
        text = text.Replace("$forwardslash;", @"/");
        return text;
    }

    public async Task<IActionResult> Search(string word, string lang)
    {
        if (word.IsNullOrWhiteSpace())
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        Search model = new()
        {
            Word = MyDecodeShashes(word),
            Lang = lang ?? ""
        };

        // superuser mode (with auxilliary glossaries etc.)
        if (IsSuper(Request)) model.Super = true;
        
        ViewData["PageTitle"] = $"\"{model.Word}\"";
			ViewData["IsSuper"] = IsSuper(Request);

        await iateBroker.DoSearchAsync(model);

        // data for Plausible analytics
        //ViewData["IsTextSearch"] = "true";
        //ViewData["IsTextSearchResultful"] = (model.exacts.Count>0 || model.relateds.Count>0 ? "true" : "false");
        //ViewData["SearchText00"] = (model.exacts.Count==0 && model.relateds.Count==0 ? model.word : "");
        //ViewData["SearchText01"] = (model.exacts.Count==0 && model.relateds.Count>0 ? model.word : "");
        //ViewData["SearchText1X"] = (model.exacts.Count>0 ? model.word : "");

        return View("IateSearch", model);
	}
}
