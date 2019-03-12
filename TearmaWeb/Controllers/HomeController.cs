using Ansa.Extensions;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace TearmaWeb.Controllers
{
    public class HomeController : Controller {
        private readonly IQueryLogger _queryLogger;

        public HomeController(IQueryLogger queryLogger) {
            _queryLogger = queryLogger;
        }

		private string myDecodeShashes(string text){
			text=text.Replace("$backslash;", @"\");
			text=text.Replace("$forwardslash;", @"/");
			return text;
		}

        public IActionResult Index() {
			Models.Home.Index model=new Models.Home.Index();
			Broker.DoIndex(model);
            ViewData["PageTitle"] = "téarma.ie";
            ViewData["TagLine"] = "An Bunachar Náisiúnta Téarmaíochta don Ghaeilge · The National Terminology Database for Irish";
            return View("Index", model);
		}

		public IActionResult Entry(int id) {
			Models.Home.Entry model=new Models.Home.Entry();
			model.id=id;
			Broker.DoEntry(model);
            ViewData["PageTitle"] = "téarma.ie";
            ViewData["TagLine"] = "An Bunachar Náisiúnta Téarmaíochta don Ghaeilge · The National Terminology Database for Irish";
            return View("Entry", model);
		}

		public IActionResult QuickSearch(string word, string lang) {
			IActionResult ret;
            if (word.IsNullOrWhiteSpace()) {
                ret = new RedirectToActionResult("Index", "Home", null);
            } else if (Regex.IsMatch(word, @"^\#[0-9]+$")){
				ret=new RedirectResult("/id/"+word.Replace("#", ""));
			} else {
                using (var stopwatch = new SimpleTimer()) {
                    Models.Home.QuickSearch model = new Models.Home.QuickSearch();
                    model.word = this.myDecodeShashes(word);
                    model.lang = lang ?? "";
                    Broker.DoQuickSearch(model);
                    ret = View("QuickSearch", model);
                    var query = new Query {
                        QueryCategory = "QuickSearch",
                        QueryTerms = word,
                        QueryText = Request.Path,
                        ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                        ResultCount = model.exacts.Count,
                        JsonData = model.searchData()
                    };
                    _queryLogger.Log(query);
                    ViewData["PageTitle"] = $"\"{model.word}\"";
                }
			}
			return ret;
		}

		public IActionResult AdvSearch(string word, string length, string extent, string lang, int posLabel, int domainID, int subdomainID, int page) {
            using (var stopwatch = new SimpleTimer()) {
                if (lang is null) lang = "";
                if (page < 1) page = 1;
                Models.Home.AdvSearch model = new Models.Home.AdvSearch();
                model.word = this.myDecodeShashes(word ?? "");
                model.length = length;
                model.extent = extent;
                if (lang != "0") model.lang = lang;
                model.posLabel = posLabel;
                model.domainID = domainID;
                model.subdomainID = subdomainID;
                model.page = page;
                if (model.word == "") {
                    Broker.PrepareAdvSearch(model);
                    ViewData["PageTitle"] = "Cuardach casta · Advanced search";
                } else {
                    Broker.DoAdvSearch(model);
                    var query = new Query {
                        QueryCategory = "AdvSearch",
                        QueryTerms = word,
                        QueryText = Request.Path,
                        ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                        ResultCount = model.matches.Count,
                        JsonData = model.searchData()
                    };
                    _queryLogger.Log(query);
                    ViewData["PageTitle"] = $"\"{model.word}\" | Cuardach casta · Advanced search";
                }

                return View("AdvSearch", model);
            }
		}

		public IActionResult Domains(string lang) {
			if(lang is null) lang="";
			Models.Home.Domains model=new Models.Home.Domains();
			model.lang=lang;
			Broker.DoDomains(model);
            ViewData["PageTitle"] = "Brabhsáil · Browse";
            return View("Domains", model);
		}

		public IActionResult Domain(int domID, int subdomID, string lang, int page) {
			if(lang is null) lang="";
			Models.Home.Domain model=new Models.Home.Domain();
			model.lang=lang;
			model.domID=domID;
			model.subdomID=subdomID;
			model.page=page;
			Broker.DoDomain(model);
            ViewData["PageTitle"] = "Brabhsáil · Browse";
            return View("Domain", model);
		}
	}
}