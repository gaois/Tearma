using Microsoft.AspNetCore.Mvc;
using BotDetect.Web.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using TearmaWeb.Models.Ask;
using System.Net.Mail;
using System.Net;

namespace TearmaWeb.Controllers
{
    public class AskController : Controller {
		private readonly IConfiguration _configuration;

		private bool isSuper(HttpRequest request) => request.Host.Host == "super.tearma.ie";

		public AskController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		[HttpGet]
		public IActionResult Ask() {
			Ask model = new Ask
			{
				mode = "empty"
			};
			ViewData["PageTitle"] = "Fiosruithe · Queries";
            IActionResult ret=View("Ask", model);
			ViewData["IsSuper"]=isSuper(Request);	
			return ret;
		}

		[HttpPost]
		public IActionResult Ask(
			string termEN,
			string termXX,
			string context,
			string def,
			string example,
			string other,
			string termGA,
			string name,
			string email,
			string phone) {
			Ask model = new Ask
			{
				termEN = termEN,
				termXX = termXX,
				context = context,
				def = def,
				example = example,
				other = other,
				termGA = termGA,
				name = name,
				email = email,
				phone = phone
			};

			if (termEN != null && context != null && def != null && name != null && (email != null || phone != null)) {
				MvcCaptcha mvcCaptcha = new MvcCaptcha("AskCaptcha");
				if(mvcCaptcha.Validate(HttpContext.Request.Form["CaptchaCode"])){
					model.mode="thanks";
					string subject = "Fiosrú téarmaíochta ó tearma.ie";
					string html = "";
					if(termEN != "") html += "AN TÉARMA BÉARLA:<br/>" + sanitiseHtml(termEN) + "<br/><br/>";
					if(termXX != "") html += "AN TÉARMA I DTEANGA(CHA) EILE:<br/>" + sanitiseHtml(termXX) + "<br/><br/>";
					if(context != "") html += "COMHTHÉACS:<br/>" + sanitiseHtml(context) + "<br/><br/>";
					if(def != "") html += "SAINMHÍNIÚ:<br/>" + sanitiseHtml(def) + "<br/><br/>";
					if(example != "") html += "SAMPLA ÚSÁIDE:<br/>" + sanitiseHtml(example) + "<br/><br/>";
					if(other != "") html += "AON EOLAS EILE:<br/>" + sanitiseHtml(other) + "<br/><br/>";
					if(termGA != "") html += "MOLADH DON TÉARMA GAEILGE:<br/>" + sanitiseHtml(termGA) + "<br/><br/>";
					if(name != "") html += "AINM:<br/>" + sanitiseHtml(name) + "<br/><br/>";
					if(email != "") html += "SEOLADH RÍOMHPHOIST:<br/>" + sanitiseHtml(email) + "<br/><br/>";
					if(phone != "") html += "UIMHIR GHUTHÁIN:<br/>" + (phone) + "<br/><br/>";

					using (var message = new MailMessage())
					using (var client = GetClient())
					{
                        message.To.Add(new MailAddress("tearmai@forasnagaeilge.ie"));
                        message.Bcc.Add(new MailAddress("tearma@dcu.ie"));
                        message.From = new MailAddress("noreply@gaois.ie", "Téarma");
						message.Subject = subject;
						message.Body = html;
						message.BodyEncoding = System.Text.Encoding.UTF8;
						message.IsBodyHtml = true;

						client.Send(message);
					}
				} else {
					model.mode="captchaError";
				}
			} else {
				model.mode="error";
            }
            ViewData["PageTitle"] = "Fiosruithe · Queries";
            IActionResult ret=View("Ask", model);
			ViewData["IsSuper"]=isSuper(Request);	
			return ret;
		}

		private string sanitiseHtml(string s) {
			if (s is null) s = "";
			return s.Replace("<", "&lt;");
		}

		private SmtpClient GetClient()
		{
			SmtpClient client = new SmtpClient();

			if (_configuration["Smtp:UserName"] != null && _configuration["Smtp:Password"] != null)
			{
				client.UseDefaultCredentials = false;
				client.Credentials = new NetworkCredential(_configuration["Smtp:UserName"], _configuration["Smtp:Password"]);
			}

			if (!string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]))
				client.Host = _configuration["Smtp:Host"];

			if (int.TryParse(_configuration["Smtp:Port"], out int port))
				client.Port = port;

			if (bool.TryParse(_configuration["Smtp:EnableSSL"], out bool enableSSL))
				client.EnableSsl = enableSSL;

			return client;
		}
	}
}