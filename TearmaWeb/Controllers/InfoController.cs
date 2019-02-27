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
			if(section=="info") model.toc=Models.Info.Toc.InfoToc();
			if(section=="help") model.toc=Models.Info.Toc.HelpToc();
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

	}
}
