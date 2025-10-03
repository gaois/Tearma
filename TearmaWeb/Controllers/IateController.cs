using Ansa.Extensions;
using Gaois.QueryLogger;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TearmaWeb.Models;
using TearmaWeb.Models.Iate;

namespace TearmaWeb.Controllers
{
    public class IateController : Controller {
		private bool isSuper(Microsoft.AspNetCore.Http.HttpRequest request) {
			//return request.Host.Host=="localhost";
			return request.Host.Host=="super.tearma.ie";
		}

		private string myDecodeShashes(string text) {
			text=text.Replace("$backslash;", @"\");
			text=text.Replace("$forwardslash;", @"/");
			return text;
		}

        public IActionResult Search(string word, string lang) {
            if(word.IsNullOrWhiteSpace()) {
              return new RedirectToActionResult("Index", "Home", null);
            }
      
            Search model = new Search();
            model.word = myDecodeShashes(word);
            model.lang = lang ?? "";
			if(this.isSuper(Request)) model.super=true; //superuser mode (with auxilliary glossaries etc.)
            ViewData["PageTitle"] = $"\"{model.word}\"";
			ViewData["IsSuper"]=this.isSuper(Request);
            // data for Plausible analytics
            //ViewData["IsTextSearch"] = "true";
            //ViewData["IsTextSearchResultful"] = (model.exacts.Count>0 || model.relateds.Count>0 ? "true" : "false");
            //ViewData["SearchText00"] = (model.exacts.Count==0 && model.relateds.Count==0 ? model.word : "");
            //ViewData["SearchText01"] = (model.exacts.Count==0 && model.relateds.Count>0 ? model.word : "");
            //ViewData["SearchText1X"] = (model.exacts.Count>0 ? model.word : "");
            return View("IateSearch", model);
		}

	}
}