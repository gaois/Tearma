using Newtonsoft.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers;

public static class Prettify
{
	// Set by Startup.cs
	public static string ContentPath { get; set; } = "";

	// ---------------------------
	// Sound files
	// ---------------------------
	private static Dictionary<string, string> GetSounds(string lang, string wording)
	{
		var result = new Dictionary<string, string>();

		if (lang != "ga")
			return result;

		var dirPath = Path.Combine(ContentPath, "wwwroot", "sounds");

		// Remove invalid filename characters
		var safeWording = string.Concat(
			wording.Split(Path.GetInvalidFileNameChars())
		);

		var pattern = "*__" + safeWording.Replace(" ", "_") + ".wav";

		if (!Directory.Exists(dirPath))
			return result;

		foreach (var filePath in Directory.GetFiles(dirPath, pattern, SearchOption.AllDirectories))
		{
			var webPath = "/sounds" + filePath[dirPath.Length..].Replace("\\", "/");
			var dialectAbbr = Path.GetFileName(filePath)[..1];

			if (!result.ContainsKey(dialectAbbr))
				result[dialectAbbr] = webPath;
		}

		return result;
	}

	// ---------------------------
	// Entry link
	// ---------------------------
	public static string EntryLink(int id, string json, string primLang)
	{
		var entry = JsonConvert.DeserializeObject<Models.Data.Entry>(json)
			?? new Models.Data.Entry();

		var leftLang = primLang;
		var rightLang = primLang == "en" ? "ga" : "en";

		var html = new StringBuilder();

		html.Append("<a class='prettyEntryLink' href='/id/")
			.Append(id)
			.Append("/'>");

		html.Append("<span class='bullet'>#</span>&nbsp;");

		var terms = new List<string>();

		foreach (var desig in entry.Desigs)
			if (desig.Term.Lang == leftLang && desig.Nonessential == 0)
				terms.Add($"<span class='term left'>{desig.Term.Wording}</span>");

		foreach (var desig in entry.Desigs)
			if (desig.Term.Lang == rightLang && desig.Nonessential == 0)
				terms.Add($"<span class='term right'>{desig.Term.Wording}</span>");

		if (terms.Count > 0)
			html.Append(string.Join(" &middot; ", terms));
		else
			html.Append(id);

		html.Append("</a>");

		return html.ToString();
	}

	// ---------------------------
	// Entry (full)
	// ---------------------------
	public static string Entry(
		int id,
		string json,
		Lookups lookups,
		string primLang)
	{
		return Entry(id, json, lookups, primLang, []);
	}

	public static string Entry(
		int id,
		string json,
		Lookups lookups,
		string primLang,
		Dictionary<int, string> xrefTargets)
	{
		var entry = JsonConvert.DeserializeObject<Models.Data.Entry>(json)
			?? new Models.Data.Entry();

		var leftLang = primLang;
		var rightLang = primLang == "en" ? "ga" : "en";

		var html = new StringBuilder();

		html.Append("<div class='prettyEntry'>");

		// Permalink
		html.Append("<a class='permalink' href='/id/")
			.Append(id)
			.Append("/'>#</a>");

		// Domains
		foreach (var dom in entry.Domains)
			html.Append(DomainAssig(dom, leftLang, rightLang, lookups));

		// Draft status
		if (entry.DStatus == "0")
		{
			var labelGa = "DRÉACHT-IONTRÁIL";
			var labelEn = "DRAFT ENTRY";

			html.Append("<div class='prettyStatus'>")
				.Append("<div class='left' lang='")
				.Append(leftLang)
				.Append("'>")
				.Append(leftLang == "ga" ? labelGa : labelEn)
				.Append("</div>")
				.Append("<div class='right' lang='")
				.Append(rightLang)
				.Append("'>")
				.Append(rightLang == "ga" ? labelGa : labelEn)
				.Append("</div>")
				.Append("<div class='clear'></div>")
				.Append("</div>");
		}

		// Desigs + intros (left)
		{
			var block = BuildDesigBlock(entry, leftLang, lookups);
			html.Append("<div class='desigBlock left'>").Append(block).Append("</div>");
		}

		// Desigs + intros (right)
		{
			var block = BuildDesigBlock(entry, rightLang, lookups);
			html.Append("<div class='desigBlock right'>").Append(block).Append("</div>");
		}

		// Other languages
		foreach (var lang in lookups.Languages)
		{
			if (lang.Abbr is not "ga" and not "en")
			{
				var block = BuildDesigBlock(entry, lang.Abbr, lookups);
                if (!string.IsNullOrEmpty(block))
                {
                    html.Append("<div class='desigBlock bottom'>")
                        .Append(block)
                        .Append("</div>");
                }
			}
		}

		// Definitions
		foreach (var def in entry.Definitions)
			html.Append(Definition(def, leftLang, rightLang, lookups));

		// Examples
		foreach (var ex in entry.Examples)
			html.Append(Example(ex, leftLang, rightLang));

		// Xrefs
		if (entry.Xrefs?.Count > 0)
		{
			var xrefHtml = BuildXrefs(entry, primLang, xrefTargets);
			if (!string.IsNullOrEmpty(xrefHtml))
				html.Append(xrefHtml);
		}

		html.Append("<div class='clear'></div>");

		html.Append("<a class='detailsIcon showDetails' style='display: none' href='javascript:void(null)' onclick='showDetails(this)'><span class='icon fas fa-angle-down'></span> <span class='ga'>Taispeáin breis sonraí</span> &middot; <span class='en'>Show more details</span></a>");
		html.Append("<a class='detailsIcon hideDetails' style='display: none' href='javascript:void(null)' onclick='hideDetails(this)'><span class='icon fas fa-angle-up'></span> <span class='ga'>Folaigh sonraí breise</span> &middot; <span class='en'>Hide details</span></a>");

		html.Append("</div>");

		return html.ToString();
	}

    public static string Desig(Models.Data.Desig desig, bool withLangLabel, Lookups lookups)
    {
        if (desig is null || desig.Term is null)
            return "";

        // Normalise all possibly-null fields up front
        var lang = desig.Term.Lang ?? "";
        var wording = desig.Term.Wording ?? "";
        var annots = desig.Term.Annots ?? [];
        var inflects = desig.Term.Inflects ?? [];
        var clarif = desig.Clarif ?? "";

        var sb = new StringBuilder();

        // Determine grey class (negative acceptability)
        var grey = "";
        if (desig.Accept is int acceptId &&
            lookups!.AcceptLabelsById.TryGetValue(acceptId, out var acceptMd) &&
            acceptMd.Level < 0)
        {
            grey = " grey";
        }

        var nonessential = desig.Nonessential == 1 ? " nonessential" : "";

        sb.Append("<div class='prettyDesig")
          .Append(grey)
          .Append(nonessential)
          .Append("' data-lang='")
          .Append(lang)
          .Append("' data-wording='")
          .Append(HtmlEncoder.Default.Encode(wording))
          .Append("'>");

        // Language label
        if (withLangLabel)
            sb.Append(Lang(lang, lookups));

        // Sound files
        var sounds = GetSounds(lang, wording);
        if (sounds.Count > 0)
        {
            sb.Append("<span class='playme' onclick='playerMenuClick(this)'");
            foreach (var kvp in sounds)
            {
                sb.Append(" data-")
                  .Append(kvp.Key)
                  .Append("='")
                  .Append(kvp.Value)
                  .Append('\'');
            }
            sb.Append("><i class=\"fas fa-volume-up\"></i></span> ");
        }

        // Wording + annotations
        sb.Append(Wording(lang, wording, annots, lookups));

        // Term menu
        if (lang is "ga" or "en")
            sb.Append("<span class='clickme' onclick='termMenuClick(this)'>▼</span>");

        // Copy icon
        sb.Append("<span class='copyme' onclick='copyClick(this)' title='Cóipeáil &middot; Copy'><i class='far fa-copy'></i><i class='fas fa-check'></i></span>");

        // Acceptability label
        if (desig.Accept is int acc)
            sb.Append(' ').Append(Accept(acc, lookups));

        // Clarification
        if (!string.IsNullOrWhiteSpace(clarif))
            sb.Append(' ').Append(Clarif(clarif));

        // Inflections
        if (inflects.Count > 0)
        {
            sb.Append("<div class='inflects'>");
            var first = true;
            foreach (var inflect in inflects)
            {
                if (!first) sb.Append(", ");
                sb.Append(Inflect(inflect, lookups));
                first = false;
            }
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private struct CharInfo
    {
        public char Character;
        public string MarkupBefore;
        public string MarkupAfter;
        public string LabelsAfter;
    }

    public static string Wording(
        string lang,
        string wording,
        List<Models.Data.Annot> annots,
        Lookups lookups)
    {
        // Build character list
        var chars = new CharInfo[wording.Length];
        for (int i = 0; i < wording.Length; i++)
            chars[i].Character = wording[i];

        int index = 0;

        foreach (var annot in annots)
        {
            if (annot.Label?.Value is not string labelValue)
            {
                index++;
                continue;
            }

            int start = Math.Max(annot.Start - 1, 0);
            int stop = annot.Stop;
            if (stop > chars.Length) stop = chars.Length;
            if (stop == 0) stop = chars.Length;

            for (int i = start; i < stop; i++)
            {
                ref var c = ref chars[i];

                switch (annot.Label.Type)
                {
                    case "posLabel":
                        if (lookups!.PosLabelsById.TryGetValue(int.Parse(labelValue), out var posMd))
                        {
                            c.MarkupBefore = $"<span class='char h{index}'>" + c.MarkupBefore;
                            c.MarkupAfter += "</span>";

                            if (i == stop - 1)
                            {
                                c.LabelsAfter +=
                                    $" <span class='label posLabel hintable' onmouseover='hon(this, {index})' onmouseout='hoff(this, {index})' title='{posMd.Name["ga"]}/{posMd.Name["en"]}'>{posMd.Abbr}</span>";
                            }
                        }
                        break;

                    case "inflectLabel":
                        if (lookups!.InflectLabelsById.TryGetValue(int.Parse(labelValue), out var inflMd))
                        {
                            c.MarkupBefore = $"<span class='char h{index}'>" + c.MarkupBefore;
                            c.MarkupAfter += "</span>";

                            if (i == stop - 1)
                            {
                                c.LabelsAfter +=
                                    $" <span class='label inflectLabel hintable' onmouseover='hon(this, {index})' onmouseout='hoff(this, {index})' title='{inflMd.Name["ga"]}/{inflMd.Name["en"]}'>{inflMd.Abbr}</span>";
                            }
                        }
                        break;

                    case "langLabel":
                        if (lookups!.LanguagesByAbbr.TryGetValue(labelValue, out var langMd))
                        {
                            c.MarkupBefore = $"<span class='char h{index}'>" + c.MarkupBefore;
                            c.MarkupAfter += "</span>";

                            if (i == stop - 1)
                            {
                                c.LabelsAfter +=
                                    $" <span class='label langLabel hintable' onmouseover='hon(this, {index})' onmouseout='hoff(this, {index})' title='{langMd.Name["ga"]}/{langMd.Name["en"]}'>{langMd.Abbr.ToUpper()}</span>";
                            }
                        }
                        break;

                    case "symbol":
                        if (labelValue != "proper")
                        {
                            c.MarkupBefore = $"<span class='char h{index}'>" + c.MarkupBefore;
                            c.MarkupAfter += "</span>";

                            var (symbol, title) = labelValue switch
                            {
                                "tm" => ("<span style='position: relative; top: -5px; font-size: 0.5em'>TM</span>", "trádmharc/trademark"),
                                "regtm" => ("®", "trádmharc cláraithe/registered trademark"),
                                "proper" => ("¶", "ainm dílis/proper noun"),
                                _ => ("", "")
                            };

                            if (i == stop - 1)
                            {
                                c.LabelsAfter +=
                                    $" <span class='label symbol hintable' onmouseover='hon(this, {index})' onmouseout='hoff(this, {index})' title='{title}'>{symbol}</span>";
                            }
                        }
                        break;

                    case "formatting":
                        c.MarkupBefore = "<span style='font-style: italic'>" + c.MarkupBefore;
                        c.MarkupAfter += "</span>";
                        break;
                }
            }

            index++;
        }

        // Build final string
        var sb = new StringBuilder();
        foreach (var c in chars)
            sb.Append(c.MarkupBefore).Append(c.Character).Append(c.MarkupAfter).Append(c.LabelsAfter);

        var encoded = Uri.EscapeDataString(
            wording.Replace("/", "$forwardslash;").Replace("\\", "$backslash;")
        );

        return $"<a class='prettyWording' href='/q/{encoded}/{lang}/'>{sb}</a>";
    }

    public static string Inflect(Models.Data.Inflect inflect, Lookups lookups)
    {
        if (lookups!.InflectLabelsById.TryGetValue(inflect.Label, out var md))
        {
            return $"<span class='inflect'><span class='abbr hintable' title='{md.Name["ga"]}/{md.Name["en"]}'>{md.Abbr}</span>&nbsp;<span class='wording'>{inflect.Text}</span></span>";
        }

        return "";
    }

    public static string Accept(int id, Lookups lookups)
    {
        if (lookups!.AcceptLabelsById.TryGetValue(id, out var md))
        {
            return $"<span class='accept'>{md.Name["ga"]}/{md.Name["en"]}</span>";
        }

        return "";
    }

    public static string Clarif(string s)
    {
        return $"<span class='clarif'>({s})</span>";
    }

    public static string Lang(string abbr, Lookups lookups)
    {
        if (lookups!.LanguagesByAbbr.TryGetValue(abbr, out var language))
        {
            return $"<span class='prettyLang hintable' title='{language.Name["ga"]}/{language.Name["en"]}'>{abbr.ToUpper()}</span>";
        }

        return "";
    }

    public static string DomainAssig(int? domID, string leftLang, string rightLang, Lookups lookups)
    {
        if (domID is null || !lookups!.DomainsById.TryGetValue(domID.Value, out var domain))
            return "";

        var stepsLeft = "";
        var stepsRight = "";
        var recursionCounter = 0;

        var current = domain;

        while (current != null)
        {
            if (!string.IsNullOrEmpty(stepsLeft))
                stepsLeft = " » " + stepsLeft;

            if (!string.IsNullOrEmpty(stepsRight))
                stepsRight = " » " + stepsRight;

            stepsLeft =
                (current.Name.TryGetValue(leftLang, out var leftName) ? leftName : current.Name["ga"])
                + stepsLeft;

            stepsRight =
                (current.Name.TryGetValue(rightLang, out var rightName) ? rightName : current.Name["en"])
                + stepsRight;

            recursionCounter++;

            current =
                current.ParentID > 0 &&
                recursionCounter < 10 &&
                lookups.DomainsById.TryGetValue(current.ParentID, out var parent)
                    ? parent
                    : null;
        }

        return
            "<div class='prettyDomain'>" +
                $"<div class='left'><a href='/dom/{domID}/{leftLang}/'>{stepsLeft}</a></div>" +
                $"<div class='right'><a href='/dom/{domID}/{rightLang}/'>{stepsRight}</a></div>" +
                "<div class='clear'></div>" +
            "</div>";
    }

    public static string DomainAssig(int? domID, string lang, Lookups lookups)
    {
        if (domID is null || !lookups!.DomainsById.TryGetValue(domID.Value, out var domain))
            return "";

        var steps = "";
        var recursionCounter = 0;
        var current = domain;

        while (current != null)
        {
            if (!string.IsNullOrEmpty(steps))
                steps = " » " + steps;

            steps =
                (current.Name.TryGetValue(lang, out var name) ? name : current.Name["ga"])
                + steps;

            recursionCounter++;

            current =
                current.ParentID > 0 &&
                recursionCounter < 10 &&
                lookups.DomainsById.TryGetValue(current.ParentID, out var parent)
                    ? parent
                    : null;
        }

        return
            "<span class='prettyDomainInline'>" +
                $"<a href='/dom/{domID}/{lang}/'>{steps}</a>" +
            "</span>";
    }

    public static string Definition(
        Models.Data.Definition def,
        string leftLang,
        string rightLang,
        Lookups lookups)
    {
        var nonessential = def.Nonessential == 1 ? " nonessential" : "";

        var sb = new StringBuilder();

        sb.Append("<div class='prettyDefinition").Append(nonessential).Append("'>");

        // Left
        sb.Append("<div class='left'>");
        foreach (var da in def.Domains)
            if (da is int id)
                sb.Append(DomainAssig(id, leftLang, lookups)).Append(' ');

        if (def.Texts.TryGetValue(leftLang, out var leftText))
            sb.Append(leftText);

        sb.Append("</div>");

        // Right
        sb.Append("<div class='right'>");
        foreach (var da in def.Domains)
            if (da is int id)
                sb.Append(DomainAssig(id, rightLang, lookups)).Append(' ');

        if (def.Texts.TryGetValue(rightLang, out var rightText))
            sb.Append(rightText);

        sb.Append("</div>");

        sb.Append("<div class='clear'></div>");
        sb.Append("</div>");

        return sb.ToString();
    }

    public static string Example(Models.Data.Example ex, string leftLang, string rightLang)
    {
        var nonessential = ex.Nonessential == 1 ? " nonessential" : "";

        var sb = new StringBuilder();

        sb.Append("<div class='prettyExample").Append(nonessential).Append("'>");

        // Left
        sb.Append("<div class='left'>");
        if (ex.Texts.TryGetValue(leftLang, out var leftTexts))
        {
            foreach (var text in leftTexts)
                sb.Append("<div class='text'>").Append(text).Append("</div>");
        }
        sb.Append("</div>");

        // Right
        sb.Append("<div class='right'>");
        if (ex.Texts.TryGetValue(rightLang, out var rightTexts))
        {
            foreach (var text in rightTexts)
                sb.Append("<div class='text'>").Append(text).Append("</div>");
        }
        sb.Append("</div>");

        sb.Append("<div class='clear'></div>");
        sb.Append("</div>");

        return sb.ToString();
    }

    private static string BuildDesigBlock(Models.Data.Entry entry, string lang, Lookups lookups)
    {
        var sb = new StringBuilder();
        var withLabel = true;

        foreach (var desig in entry.Desigs)
        {
            if (desig.Term.Lang == lang)
            {
                sb.Append(Desig(desig, withLabel, lookups));
                withLabel = false;
            }
        }

        if (entry.Intros.TryGetValue(lang, out var intro) && !string.IsNullOrWhiteSpace(intro))
            sb.Append("<div class='intro'><span>(").Append(intro).Append(")</span></div>");

        return sb.ToString();
    }

    private static string BuildXrefs(
        Models.Data.Entry entry,
        string primLang,
        Dictionary<int, string> xrefTargets)
    {
        var sb = new StringBuilder();
        var count = 0;

        sb.Append("<div class='prettyXrefs'>")
          .Append("<div class='title'><span class='title'><span class='ga' lang='ga'>FÉACH FREISIN</span> &middot; <span class='en' lang='en'>SEE ALSO</span></span></div>");

        foreach (var id in entry.Xrefs)
        {
            if (xrefTargets.TryGetValue(id, out var json))
            {
                sb.Append(" <span class='xref'>")
                  .Append(EntryLink(id, json, primLang))
                  .Append("</span>");
                count++;
            }
        }

        sb.Append("</div>");

        return count > 0 ? sb.ToString() : "";
    }
}

/// <summary>
/// Converts "faulty" integers such as "9+", "1o", "1O", "12?" into valid integers.
/// - Replaces 'o'/'O' with '0'
/// - Strips all non-digit characters
/// - Returns null if no digits remain
/// </summary>
public partial class IntegerJsonConverter : JsonConverter<int?>
{
    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();

    public override bool CanWrite => true;
    public override bool CanRead => true;

    public override int? ReadJson(
        JsonReader reader,
        Type objectType,
        int? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
                return null;

            case JsonToken.Integer:
                return Convert.ToInt32(reader.Value);

            case JsonToken.String:
                var text = (reader.Value as string) ?? "";
                text = text.Replace("o", "0").Replace("O", "0");
                text = NonDigitRegex().Replace(text, "");

                if (string.IsNullOrWhiteSpace(text))
                    return null;

                return int.TryParse(text, out var num) ? num : null;

            default:
                return null;
        }
    }

    public override void WriteJson(JsonWriter writer, int? value, JsonSerializer serializer)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteNull();
    }
}
