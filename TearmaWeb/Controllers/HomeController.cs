using Ansa.Extensions;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers
{
    public class HomeController : Controller {
        private readonly IQueryLogger _queryLogger;
        private readonly Broker _broker;

        public HomeController(IQueryLogger queryLogger, Broker broker) {
            _queryLogger = queryLogger;
            _broker = broker;
        }

		private string myDecodeShashes(string text) {
			text=text.Replace("$backslash;", @"\");
			text=text.Replace("$forwardslash;", @"/");
			return text;
		}

        public IActionResult Index() {
			Index model=new Index();
            _broker.DoIndex(model);
			return View("Index", model);
		}

		public IActionResult Entry(int id) {
            Entry model=new Entry();
			model.id=id;
            _broker.DoEntry(model);
			return View("Entry", model);
		}

		public IActionResult QuickSearch(string word, string lang) {
			if(Regex.IsMatch(word, @"^\#[0-9]+$")) {
				return new RedirectResult("/id/"+word.Replace("#", ""));
            }

            using (var stopwatch = new SimpleTimer()) {
                QuickSearch model = new QuickSearch();
                model.word = myDecodeShashes(word);
                model.lang = lang ?? "";
                _broker.DoQuickSearch(model);
                var query = new Query {
                    QueryCategory = "QuickSearch",
                    QueryTerms = word,
                    QueryText = Request.Path,
                    ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                    ResultCount = model.exacts.Count,
                    JsonData = model.searchData()
                };
                _queryLogger.Log(query);
                return View("QuickSearch", model);
            }
		}

		public IActionResult AdvSearch(string word, string length, string extent, string lang, int posLabel, int domainID, int subdomainID, int page) {
            using (var stopwatch = new SimpleTimer()) {
                if (lang is null) lang = "";
                if (page < 1) page = 1;
                AdvSearch model = new AdvSearch();
                model.word = this.myDecodeShashes(word ?? "");
                model.length = length;
                model.extent = extent;
                if (lang != "0") model.lang = lang;
                model.posLabel = posLabel;
                model.domainID = domainID;
                model.subdomainID = subdomainID;
                model.page = page;
                if (model.word.IsNullOrWhiteSpace()) {
                    _broker.PrepareAdvSearch(model);
                } else {
                    _broker.DoAdvSearch(model);
                    var query = new Query {
                        QueryCategory = "AdvSearch",
                        QueryTerms = word,
                        QueryText = Request.Path,
                        ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                        ResultCount = model.matches.Count,
                        JsonData = model.searchData()
                    };
                    _queryLogger.Log(query);
                }

                return View("AdvSearch", model);
            }
		}

		public IActionResult Domains(string lang) {
			if(lang is null) lang="";
            Domains model=new Domains();
			model.lang=lang;
            _broker.DoDomains(model);
			return View("Domains", model);
		}

		public IActionResult Domain(int domID, int subdomID, string lang, int page) {
			if(lang is null) lang="";
            Domain model=new Domain();
			model.lang=lang;
			model.domID=domID;
			model.subdomID=subdomID;
			model.page=page;
            _broker.DoDomain(model);
			return View("Domain", model);
		}
	}
}