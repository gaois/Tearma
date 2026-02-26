using Microsoft.AspNetCore.Mvc;
using TearmaWeb.Models.Widgets;

namespace TearmaWeb.Controllers;

public class WidgetsController(Broker broker) : Controller
{
    public IActionResult Box()
    {
        var model = new Box();
        return View("Box", model);
    }

    public async Task<IActionResult> Tod()
    {
        var model = new Tod();

        await broker.DoTodAsync(model);

        return View("Tod", model);
    }
}
