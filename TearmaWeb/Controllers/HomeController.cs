using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TearmaWeb.Controllers {
	public class HomeController : Controller {

		public IActionResult Index() {
			return View();
		}

		public IActionResult Entry(int id) {
			ViewData["id"]=id;
			return View();
		}

		public IActionResult QuickSearch(string word, string lang) {
			Models.Home.QuickSearch model=new Models.Home.QuickSearch {word=word, lang=lang??""};
			return View("QuickSearch", model);
		}

		public IActionResult AdvSearch(string word, string length, string extent, string lang, int page) {
			if(lang is null) lang="";
			if(page<1) page=1;
			Models.Home.AdvSearch model=new Models.Home.AdvSearch {word=word??"", length=length, extent=extent, lang=lang, page=page, pager=new Models.Home.Pager(page, 7)};
			return View("AdvSearch", model);
		}

		public IActionResult Domains(string lang) {
			if(lang is null) lang="";
			Models.Home.Domains model=new Models.Home.Domains { lang=lang };
			return View("Domains", model);
		}

		public IActionResult Domain(int domID, int subdomID, string lang, int page) {
			if(lang is null) lang="";
			Models.Home.Domain model=new Models.Home.Domain { lang=lang, domID=domID, subdomID=subdomID, page=page, pager=new Models.Home.Pager(page, 7) };
			return View("Domain", model);
		}


	}
}
