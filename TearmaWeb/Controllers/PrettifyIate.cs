using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TearmaWeb.Models.Iate;

namespace TearmaWeb.Controllers;

public class PrettifyIate
{
    private static string RemoveSomeHtml(string s)
    {
        return Regex.Replace(
            s, "<(?!b>|/b>|i>|/i>|strong>|/strong>|em>|/em>)[^>]+>", "", RegexOptions.IgnoreCase);
    }

    private static string RemoveAllHtml(string s)
    {
        return Regex.Replace(s, "<[^>]+>", "");
    }

    public static string Entry(IateEntry entry, string leftlang, string rightlang)
    {
        var sb = new StringBuilder();
        sb.Append("<div class='prettyEntry'>");

        // Header link
        sb.Append($"<a class='iateLink' target='_blank' href='https://iate.europa.eu/entry/result/{entry.Id}'>#{entry.Id} <i class=\"fas fa-external-link-alt\"></i></a>");

        // Domains
        if (entry.Domains != null)
        {
            foreach (var domainWrap in entry.Domains)
            {
                var domain = domainWrap.Domain;
                if (domain == null) continue;

                sb.Append("<div class='prettyDomain iate'>");

                if (domain.Path != null)
                {
                    foreach (var step in domain.Path)
                    {
                        sb.Append(WebUtility.HtmlEncode(step));
                        sb.Append(" » ");
                    }
                }

                sb.Append(WebUtility.HtmlEncode(domain.Name));
                sb.Append("</div>");
            }
        }

        var sLeft = new StringBuilder();
        var sRight = new StringBuilder();

        foreach (var lang in new[] { "ga", "en", "fr", "de" })
        {
            var block = RenderLanguageBlock(entry, lang);

            if (lang == leftlang)
            {
                sLeft.Append(block);
            }
            else if (lang == rightlang)
            {
                sRight.Append(block);
            }
            else if (block.Length > 0)
            {
                if (leftlang == "en")
                    sLeft.Append($"<div class='desigBlock bottom'>{block}</div>");
                else if (rightlang == "en")
                    sRight.Append($"<div class='desigBlock bottom'>{block}</div>");
            }
        }

        sb.Append($"<div class='desigBlock left'>{sLeft}</div>");
        sb.Append($"<div class='desigBlock right'>{sRight}</div>");
        sb.Append("<div class='clear'></div>");
        sb.Append("</div>");

        return sb.ToString();
    }

    private static string RenderLanguageBlock(IateEntry entry, string lang)
    {
        if (entry.Language == null || !entry.Language.TryGetValue(lang, out var langBlock) || langBlock.TermEntries == null)
            return "";

        var sb = new StringBuilder();
        bool isFirst = true;

        foreach (var term in langBlock.TermEntries)
        {
            var wording = RemoveAllHtml(term.TermValue ?? "");
            var encoded = WebUtility.HtmlEncode(wording);

            sb.Append($"<div class='prettyDesig' data-lang='{lang}' data-wording='{encoded}'>");

            if (isFirst)
            {
                sb.Append(LanguageLabel(lang));
            }

            if (lang == "ga" || lang == "en")
            {
                var safeUrl = Uri.EscapeDataString(
                    wording.Replace("/", "$forwardslash;").Replace("\\", "$backslash;"));
                sb.Append($"<a class='prettyWording' href='/q/{safeUrl}'>");
                sb.Append(encoded);
                sb.Append("</a>");
                sb.Append("<span class='clickme' onclick='termMenuClick(this)'>▼</span>");
                sb.Append("<span class='copyme' onclick='copyClick(this)' title='Cóipeáil · Copy'><i class='far fa-copy'></i><i class='fas fa-check'></i></span>");
            }
            else
            {
                sb.Append($"<span class='prettyWording'>{encoded}</span>");
            }

            if (lang == "ga" && term.Contexts != null)
            {
                foreach (var ctx in term.Contexts)
                {
                    sb.Append($"<div class='iateExample'>{RemoveSomeHtml(ctx.Context ?? "")}</div>");
                }
            }

            sb.Append("</div>");

            isFirst = false;
        }

        if ((lang == "ga" || lang == "en") && langBlock.Definition?.Value != null)
        {
            sb.Append($"<div class='iateDefinition'>{RemoveSomeHtml(langBlock.Definition.Value)}</div>");
        }

        return sb.ToString();
    }

    private static string LanguageLabel(string lang)
    {
        return lang switch
        {
            "ga" => "<span class='prettyLang hintable' title='Gaeilge/Irish'>GA</span>",
            "en" => "<span class='prettyLang hintable' title='Béarla/English'>EN</span>",
            "de" => "<span class='prettyLang hintable' title='Gearmáinis/German'>DE</span>",
            "fr" => "<span class='prettyLang hintable' title='Fraincis/French'>FR</span>",
            _ => ""
        };
    }
}
