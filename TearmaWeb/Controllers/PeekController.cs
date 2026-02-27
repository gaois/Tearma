using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TearmaWeb.Models;

namespace TearmaWeb.Controllers;

public class PeekController(Broker broker, IateBroker iateBroker) : Controller
{
    [OutputCache]
    public async Task<IActionResult> PeekTearma([FromQuery] string word) {
        PeekResult pr = new()
        {
            Word = word
        };

        await broker.PeekAsync(pr);

        return Json(pr);
    }

    [OutputCache]
    public async Task<IActionResult> PeekIate([FromQuery] string word) {
        PeekResult pr = new()
        {
            Word = word
        };
        
        await iateBroker.PeekAsync(pr);

        return Json(pr);
    }
}
