using Microsoft.AspNetCore.Mvc;
using BotDetect.Web.Mvc;
using TearmaWeb.Models.Ask;
using System.Net.Mail;
using System.Net;

namespace TearmaWeb.Controllers;

public class AskController(IConfiguration configuration) : Controller
{
    private static bool IsSuper(HttpRequest request) =>
        request.Host.Host.Equals("super.tearma.ie", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public IActionResult Ask()
    {
        var model = new Ask
        {
            Mode = "empty"
        };

        ViewData["PageTitle"] = "Fiosruithe · Queries";
        ViewData["IsSuper"] = IsSuper(Request);

        return View("Ask", model);
    }

    [HttpPost]
    public IActionResult Ask(
        string? termEN,
        string? termXX,
        string? context,
        string? def,
        string? example,
        string? other,
        string? termGA,
        string? name,
        string? email,
        string? phone)
    {
        var model = new Ask
        {
            TermEN = termEN,
            TermXX = termXX,
            Context = context,
            Def = def,
            Example = example,
            Other = other,
            TermGA = termGA,
            Name = name,
            Email = email,
            Phone = phone
        };

        if (!string.IsNullOrWhiteSpace(termEN) &&
            !string.IsNullOrWhiteSpace(context) &&
            !string.IsNullOrWhiteSpace(def) &&
            !string.IsNullOrWhiteSpace(name) &&
            (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(phone)))
        {
            var captcha = new MvcCaptcha("AskCaptcha");

            if (captcha.Validate(HttpContext.Request.Form["CaptchaCode"]))
            {
                model.Mode = "thanks";

                var subject = "Fiosrú téarmaíochta ó tearma.ie";

                var html = BuildEmailBody(
                    termEN,
                    termXX,
                    context,
                    def,
                    example,
                    other,
                    termGA,
                    name,
                    email,
                    phone);

                using var message = new MailMessage();
                using var client = CreateSmtpClient();

                message.To.Add("tearmai@forasnagaeilge.ie");
                message.Bcc.Add("tearma@dcu.ie");
                message.From = new MailAddress("noreply@gaois.ie", "Téarma");
                message.Subject = subject;
                message.Body = html;
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.IsBodyHtml = true;

                client.Send(message);
            }
            else
            {
                model.Mode = "captchaError";
            }
        }
        else
        {
            model.Mode = "error";
        }

        ViewData["PageTitle"] = "Fiosruithe · Queries";
        ViewData["IsSuper"] = IsSuper(Request);

        return View("Ask", model);
    }

    private static string BuildEmailBody(
        string? termEN,
        string? termXX,
        string? context,
        string? def,
        string? example,
        string? other,
        string? termGA,
        string? name,
        string? email,
        string? phone)
    {
        static string Encode(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Replace("<", "&lt;");

        var html = "";

        if (!string.IsNullOrWhiteSpace(termEN))
            html += $"AN TÉARMA BÉARLA:<br/>{Encode(termEN)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(termXX))
            html += $"AN TÉARMA I DTEANGA(CHA) EILE:<br/>{Encode(termXX)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(context))
            html += $"COMHTHÉACS:<br/>{Encode(context)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(def))
            html += $"SAINMHÍNIÚ:<br/>{Encode(def)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(example))
            html += $"SAMPLA ÚSÁIDE:<br/>{Encode(example)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(other))
            html += $"AON EOLAS EILE:<br/>{Encode(other)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(termGA))
            html += $"MOLADH DON TÉARMA GAEILGE:<br/>{Encode(termGA)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(name))
            html += $"AINM:<br/>{Encode(name)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(email))
            html += $"SEOLADH RÍOMHPHOIST:<br/>{Encode(email)}<br/><br/>";

        if (!string.IsNullOrWhiteSpace(phone))
            html += $"UIMHIR GHUTHÁIN:<br/>{Encode(phone)}<br/><br/>";

        return html;
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient();

        var host = configuration["Smtp:Host"];
        var username = configuration["Smtp:UserName"];
        var password = configuration["Smtp:Password"];
        var port = configuration["Smtp:Port"];
        var enableSsl = configuration["Smtp:EnableSSL"];

        if (!string.IsNullOrWhiteSpace(username) &&
            !string.IsNullOrWhiteSpace(password))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(username, password);
        }

        if (!string.IsNullOrWhiteSpace(host))
            client.Host = host;

        if (int.TryParse(port, out var parsedPort))
            client.Port = parsedPort;

        if (bool.TryParse(enableSsl, out var ssl))
            client.EnableSsl = ssl;

        return client;
    }
}
