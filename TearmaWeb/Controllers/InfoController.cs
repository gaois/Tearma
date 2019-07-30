using Microsoft.AspNetCore.Mvc;

namespace TearmaWeb.Controllers
{
    public class InfoController : Controller {
		public IActionResult Topic(string section, string nickname, string lang) {
			IActionResult ret;
			Models.Info.Topic model=new Models.Info.Topic();
			if(section=="eolas") {
                model.toc=Models.Info.Toc.InfoToc();
                ViewData["PageTitle"] = "Eolas · About";
            }
			if(section=="cabhair") {
                model.toc=Models.Info.Toc.HelpToc();
                ViewData["PageTitle"] = "Cabhair · Help";
            }
			if(nickname==null){
				ret=new RedirectResult("/"+section+"/"+model.toc[0].nickname+"."+lang);
			} else {
				model.section=section;
				model.nickname=nickname;
				model.lang=lang;
				string path=@"./wwwroot/"+section+"/"+nickname+"."+lang+".md";
				if(System.IO.File.Exists(path)) {
                    model.body=System.IO.File.ReadAllText(path);
                    ret=View("Topic", model);
                } else {
                    ret=new NotFoundResult();
                }
			}
			return ret;
		}

		public IActionResult Download() {
			Models.Info.Download model=new Models.Info.Download();
            ViewData["PageTitle"] = "Liostaí le híoslódáil · Downloadable Lists";
            IActionResult ret=View("Download", model);
			return ret;
		}

		public IActionResult Widgets() {
			Models.Info.Widgets model=new Models.Info.Widgets();
            ViewData["PageTitle"] = "Ábhar do shuíomhanna eile · Content for other websites";
            IActionResult ret=View("Widgets", model);
			return ret;
		}
	}
}