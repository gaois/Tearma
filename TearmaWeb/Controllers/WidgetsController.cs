using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace TearmaWeb.Controllers {
	public class WidgetsController : Controller {

		public IActionResult Box() {
			Models.Widgets.Box model=new Models.Widgets.Box();
			IActionResult ret=View("Box", model);
			return ret;
		}

		public IActionResult Tod() {
			Models.Widgets.Tod model=new Models.Widgets.Tod();
			Broker.DoTod(model);
			IActionResult ret=View("Tod", model);
			return ret;
		}

	}
}
