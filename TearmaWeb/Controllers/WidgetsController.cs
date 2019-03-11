using Microsoft.AspNetCore.Mvc;
using TearmaWeb.Models.Widgets;

namespace TearmaWeb.Controllers
{
    public class WidgetsController : Controller {
        private readonly Broker _broker;

        public WidgetsController(Broker broker) {
            _broker = broker;
        }

		public IActionResult Box() {
			Box model=new Box();
			return View("Box", model);
        }

		public IActionResult Tod() {
			Tod model=new Tod();
            _broker.DoTod(model);
			return View("Tod", model);
        }
	}
}