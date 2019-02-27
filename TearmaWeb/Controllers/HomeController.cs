using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace TearmaWeb.Controllers {
	public class HomeController : Controller {

		public IActionResult Index() {
			Models.Home.Index model=new Models.Home.Index();
			Broker.DoIndex(model);
			return View("Index", model);
		}

		public IActionResult Entry(int id) {
			Models.Home.Entry model=new Models.Home.Entry();
			model.id=id;
			Broker.DoEntry(model);
			return View("Entry", model);
		}

		public IActionResult QuickSearch(string word, string lang) {
			IActionResult ret;
			if(Regex.IsMatch(word, @"^\#[0-9]+$")){
				ret=new RedirectResult("/id/"+word.Replace("#", ""));
			} else {
				Models.Home.QuickSearch model=new Models.Home.QuickSearch();
				model.word=word;
				model.lang=lang ?? "";
				Broker.DoQuickSearch(model);
				ret=View("QuickSearch", model);
			}
			return ret;
		}

		public IActionResult AdvSearch(string word, string length, string extent, string lang, int posLabel, int domainID, int subdomainID, int page) {
			if(lang is null) lang="";
			if(page<1) page=1;
			Models.Home.AdvSearch model=new Models.Home.AdvSearch(); 
			model.word=word ?? "";
			model.length=length;
			model.extent=extent;
			if(lang!="0") model.lang=lang;
			model.posLabel=posLabel;
			model.domainID=domainID;
			model.subdomainID=subdomainID;
			model.page=page;
			if(model.word=="") Broker.PrepareAdvSearch(model); else Broker.DoAdvSearch(model);
			return View("AdvSearch", model);
		}

		public IActionResult Domains(string lang) {
			if(lang is null) lang="";
			Models.Home.Domains model=new Models.Home.Domains();
			model.lang=lang;
			Broker.DoDomains(model);
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
			return View("Domain", model);
		}


	}
}
