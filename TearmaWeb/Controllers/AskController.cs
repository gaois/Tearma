using Microsoft.AspNetCore.Mvc;
using BotDetect.Web.Mvc;

namespace TearmaWeb.Controllers
{
    public class AskController : Controller {
		private bool isSuper(Microsoft.AspNetCore.Http.HttpRequest request) {
			return request.Host.Host=="super.tearma.ie";
		}

		[HttpGet]
		public IActionResult Ask() {
			Models.Ask.Ask model=new Models.Ask.Ask();
			model.mode="empty";
            ViewData["PageTitle"] = "Fiosruithe · Queries";
            IActionResult ret=View("Ask", model);
			ViewData["IsSuper"]=this.isSuper(Request);	
			return ret;
		}

		[HttpPost]
		public IActionResult Ask(string termEN, string termXX, string context, string def, string example, string other, string termGA, string name, string email, string phone) {
			Models.Ask.Ask model=new Models.Ask.Ask();
			model.termEN=termEN;
			model.termXX=termXX;
			model.context=context;
			model.def=def;
			model.example=example;
			model.other=other;
			model.termGA=termGA;
			model.name=name;
			model.email=email;
			model.phone=phone;
			if(termEN != null && context != null && def != null && name != null && (email != null || phone != null)) {

				MvcCaptcha mvcCaptcha = new MvcCaptcha("AskCaptcha");
				if(mvcCaptcha.Validate(HttpContext.Request.Form["CaptchaCode"])){
					model.mode="thanks";
					string subject = "Fiosrú téarmaíochta ó tearma.ie";
					string html = "";
					if(termEN != "") html += "AN TÉARMA BÉARLA:<br/>" + termEN + "<br/><br/>";
					if(termXX != "") html += "AN TÉARMA I DTEANGA(CHA) EILE:<br/>" + termXX + "<br/><br/>";
					if(context != "") html += "COMHTHÉACS:<br/>" + context + "<br/><br/>";
					if(def != "") html += "SAINMHÍNIÚ:<br/>" + def + "<br/><br/>";
					if(example != "") html += "SAMPLA ÚSÁIDE:<br/>" + example + "<br/><br/>";
					if(other != "") html += "AON EOLAS EILE:<br/>" + other + "<br/><br/>";
					if(termGA != "") html += "MOLADH DON TÉARMA GAEILGE:<br/>" + termGA + "<br/><br/>";
					if(name != "") html += "AINM:<br/>" + name + "<br/><br/>";
					if(email != "") html += "SEOLADH RÍOMHPHOIST:<br/>" + email + "<br/><br/>";
					if(phone != "") html += "UIMHIR GHUTHÁIN:<br/>" + phone + "<br/><br/>";
					SendEmail("tearmai@forasnagaeilge.ie", subject, html);
				} else {
					model.mode="captchaError";
				}
			} else {
				model.mode="error";
            }
            ViewData["PageTitle"] = "Fiosruithe · Queries";
            IActionResult ret=View("Ask", model);
			ViewData["IsSuper"]=this.isSuper(Request);	
			return ret;
		}

		public static void SendEmail(string to, string subject, string body)
		{
			//System.Net.Mail.MailMessage msg=new System.Net.Mail.MailMessage("noreply@tearma.ie", to, subject, body);
			System.Net.Mail.MailMessage msg=new System.Net.Mail.MailMessage("noreply@tearma.ie", to, subject, body);
			msg.IsBodyHtml=true;
			msg.Bcc.Add("tearma@dcu.ie");
			msg.BodyEncoding=System.Text.Encoding.UTF8;
			System.Net.Mail.SmtpClient client=new System.Net.Mail.SmtpClient("localhost");
			client.Send(msg);
		}
	}
}