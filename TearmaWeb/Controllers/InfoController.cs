using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace TearmaWeb.Controllers {
	public class InfoController : Controller {

		public IActionResult Topic(string section, string nickname, string lang) {
			IActionResult ret;
			Models.Info.Topic model=new Models.Info.Topic();
			if(section=="eolas") model.toc=Models.Info.Toc.InfoToc();
			if(section=="cabhair") model.toc=Models.Info.Toc.HelpToc();
			if(nickname==null){
				ret=new RedirectResult("/"+section+"/"+model.toc[0].nickname+"."+lang);
			} else {
				model.section=section;
				model.nickname=nickname;
				model.lang=lang;
				string path=@"./wwwroot/"+section+"/"+nickname+"."+lang+".md";
				if(System.IO.File.Exists(path)) model.body=System.IO.File.ReadAllText(path);
				ret=View("Topic", model);
			}
			return ret;
		}

		public IActionResult Download() {
			Models.Info.Download model=new Models.Info.Download();
			IActionResult ret=View("Download", model);
			return ret;
		}

		public IActionResult Widgets() {
			Models.Info.Widgets model=new Models.Info.Widgets();
			IActionResult ret=View("Widgets", model);
			return ret;
		}

	}
}
