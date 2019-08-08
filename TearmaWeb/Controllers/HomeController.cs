using Ansa.Extensions;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers
{
    public class HomeController : Controller {
        private readonly IQueryLogger _queryLogger;
        private readonly Broker _broker;
		private bool isSuper(Microsoft.AspNetCore.Http.HttpRequest request) {
			//return request.Host.Host=="localhost";
			return request.Host.Host=="super.tearma.ie";
		}

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
            ViewData["PageTitle"] = "téarma.ie";
            ViewData["TagLine"] = "An Bunachar Náisiúnta Téarmaíochta don Ghaeilge · The National Terminology Database for Irish";
			ViewData["IsSuper"]=this.isSuper(Request);	
			return View("Index", model);
		}

		public IActionResult Entry(int id) {
            Entry model=new Entry();
			model.id=id;
            _broker.DoEntry(model);
            ViewData["PageTitle"] = "téarma.ie";
            ViewData["TagLine"] = "An Bunachar Náisiúnta Téarmaíochta don Ghaeilge · The National Terminology Database for Irish";
			ViewData["IsSuper"]=this.isSuper(Request);
			return View("Entry", model);
		}

		public IActionResult QuickSearch(string word, string lang) {
            if(word.IsNullOrWhiteSpace()) {
              return new RedirectToActionResult("Index", "Home", null);
            }
      
			if(Regex.IsMatch(word, @"^\#[0-9]+$")) {
				return new RedirectResult("/id/"+word.Replace("#", ""));
            }

            using (var stopwatch = new SimpleTimer()) {
                QuickSearch model = new QuickSearch();
                model.word = myDecodeShashes(word);
                model.lang = lang ?? "";
				if(this.isSuper(Request)) model.super=true; //superuser mode (with auxilliary glossaries etc.)
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
                ViewData["PageTitle"] = $"\"{model.word}\"";
				ViewData["IsSuper"]=this.isSuper(Request);
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
                    ViewData["PageTitle"] = "Cuardach casta · Advanced search";
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
                    ViewData["PageTitle"] = $"\"{model.word}\" | Cuardach casta · Advanced search";
                }

				ViewData["IsSuper"]=this.isSuper(Request);	
                return View("AdvSearch", model);
            }
		}

		public IActionResult Subdoms(int domID) {
			//_broker.DoAdvSearch(model);
			var ret=_broker.GetSubdoms(domID);
			return Content(ret);
		}

		public IActionResult Domains(string lang) {
			if(lang is null) lang="";
            Domains model=new Domains();
			model.lang=lang;
			_broker.DoDomains(model);
            ViewData["PageTitle"] = "Brabhsáil · Browse";
			ViewData["IsSuper"]=this.isSuper(Request);
			return View("Domains", model);
		}

		public IActionResult Domain(int domID, int subdomID, string lang, int page) {
            using (var stopwatch = new SimpleTimer()) {
			    if(lang is null) lang="";
                Domain model=new Domain();
			    model.lang=lang;
			    model.domID=domID;
			    model.subdomID=subdomID;
			    model.page=page;
			    _broker.DoDomain(model);
                var query = new Query {
                    QueryCategory = "Domain",
                    QueryTerms = (subdomID > 0) ? subdomID.ToString() : domID.ToString(),
                    QueryText = Request.Path,
                    ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                    ResultCount = model.matches.Count,
                    JsonData = model.searchData()
                };
                _queryLogger.Log(query);
                ViewData["PageTitle"] = "Brabhsáil · Browse";
				ViewData["IsSuper"]=this.isSuper(Request);
				return View("Domain", model);
            }
        }

        public IActionResult Error(int? code) {
            var model = new Models.ErrorModel() {
                HttpStatusCode = code ?? HttpContext.Response.StatusCode,
                RequestID = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            switch (model.HttpStatusCode) {
                case 404:
                    ViewData["PageTitle"] = "Earráid 404 · Error 404";
                    ViewData["MetaDescription"] = "Níor aimsíodh an leathanach · Page not found";
                    break;
                default:
                    ViewData["PageTitle"] = "Earráid · Error";
                    ViewData["MetaDescription"] = "Tharla earráid agus an leathanach seo á oscailt · An error occurred while attempting to open this page";
                    break;
            }
			ViewData["IsSuper"]=this.isSuper(Request);
			return View(model);
        }
	}
}