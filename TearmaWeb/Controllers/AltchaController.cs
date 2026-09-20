using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TearmaWeb.Models;
using Ixnas.AltchaNet;

namespace TearmaWeb.Controllers;

public class AltchaController(AltchaBroker altchaBroker) : Controller
{
    private static bool IsSuper(HttpRequest request)
    {
        return request.Host.Host == "super.tearma.ie";
    }

    public IActionResult GetChallenge()
    {
        var challenge = altchaBroker.generateChallenge();
        return Json(challenge);
    }

    public IActionResult AltchaWall([FromQuery] string returnUrl)
    {
        Models.Altcha.AltchaWall model = new();
        model.returnUrl = returnUrl;
        ViewData["PageTitle"] = "Fíorú slándála · Security verification";
        IActionResult ret = View("AltchaWall", model);
        ViewData["IsSuper"] = IsSuper(Request);
        ViewData["NoFooterAltcha"] = true;
        return ret;
    }


}
