using Microsoft.AspNetCore.Mvc;
using TearmaWeb.Models;

namespace TearmaWeb.Controllers;

public class PeekController(Broker broker, IateBroker iateBroker) : Controller
{
    public async Task<IActionResult> PeekTearma([FromQuery] string word) {
        PeekResult pr = new()
        {
            Word = word
        };

        await broker.PeekAsync(pr);

        return Json(pr);
    }

    public async Task<IActionResult> PeekIate([FromQuery] string word) {
        PeekResult pr = new()
        {
            Word = word
        };
        
        await iateBroker.PeekAsync(pr);

        return Json(pr);
    }
}
