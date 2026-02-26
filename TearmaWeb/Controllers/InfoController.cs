using Microsoft.AspNetCore.Mvc;

namespace TearmaWeb.Controllers;

public class InfoController : Controller
{
    private static bool IsSuper(HttpRequest request)
    {
        return request.Host.Host == "super.tearma.ie";
    }

    public IActionResult Topic(string section, string nickname, string lang = "ga")
    {
        IActionResult ret;
        Models.Info.Topic model = new();

        if (section == "eolas")
        {
            model.Toc = Models.Info.Toc.InfoToc();
            ViewData["PageTitle"] = "Eolas · About";
        }
        
        if (section == "cabhair")
        {
            model.Toc = Models.Info.Toc.HelpToc();
            ViewData["PageTitle"] = "Cabhair · Help";
        }

        if (nickname == null)
        {
            ret = new RedirectResult($"/{section}/{model.Toc[0].Nickname}.{lang}");
        }
        else
        {
            model.Section = section;
            model.Nickname = nickname;
            model.Lang = lang;
            
            string path = @"./wwwroot/" + section + "/" + nickname + "." + lang + ".md";

            if (System.IO.File.Exists(path))
            {
                model.Body = System.IO.File.ReadAllText(path);
                string altLang = (lang == "ga") ? "en" : "ga";
                ViewData["AltLang"] = altLang;
                ViewData["AlternateUrl"] = $"https://www.tearma.ie/{section}/{nickname}.{altLang}";
                ret = View("Topic", model);
            }
            else
            {
                ret = new NotFoundResult();
            }
        }

        ViewData["IsSuper"] = IsSuper(Request);

        return ret;
    }

    public IActionResult Download()
    {
        Models.Info.Download model = new();
        ViewData["PageTitle"] = "Liostaí le híoslódáil · Downloadable Lists";
        IActionResult ret = View("Download", model);
        ViewData["IsSuper"] = IsSuper(Request);
        return ret;
    }

    public IActionResult Widgets()
    {
        Models.Info.Widgets model = new();
        ViewData["PageTitle"] = "Ábhar do shuíomhanna eile · Content for other websites";
        IActionResult ret = View("Widgets", model);
        ViewData["IsSuper"] = IsSuper(Request);
        return ret;
    }
}