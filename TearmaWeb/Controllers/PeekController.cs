using Ansa.Extensions;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TearmaWeb.Models;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers {
    public class PeekController : Controller {
        private readonly IQueryLogger _queryLogger;
        private readonly Broker _broker;

        public PeekController(IQueryLogger queryLogger, Broker broker) {
            _queryLogger = queryLogger;
            _broker = broker;
        }

        public IActionResult PeekTearma([FromQuery] string word) {
            PeekResult pr = new PeekResult();
            pr.word = word;
            _broker.Peek(pr);
            return Json(pr);
        }

        public IActionResult PeekIate([FromQuery] string word) {
            PeekResult pr = new PeekResult();
            pr.word = word;
            return Json(pr);
        }

    }
}
