using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TearmaWeb.Models.Home;

public static class Tools
{
    public static string SlashEncode(string text)
    {
        return text
            .Replace("%", "%25")
            .Replace("\\", "$backslash;")
            .Replace("/", "$forwardslash;");
    }
}

/// <summary>Represents languages and metadata.</summary>
public class Lookups
{
    public List<Language> Languages { get; set; } = [];
    public Dictionary<string, Language> LanguagesByAbbr { get; set; } = [];

    public void AddLanguage(Language language)
    {
        Languages.Add(language);
        LanguagesByAbbr[language.Abbr] = language;
    }

    public List<Metadatum> AcceptLabels { get; set; } = [];
    public List<Metadatum> InflectLabels { get; set; } = [];
    public List<Metadatum> PosLabels { get; set; } = [];
    public List<Metadatum> Domains { get; set; } = [];

    public Dictionary<int, Metadatum> AcceptLabelsById { get; set; } = [];
    public Dictionary<int, Metadatum> InflectLabelsById { get; set; } = [];
    public Dictionary<int, Metadatum> PosLabelsById { get; set; } = [];
    public Dictionary<int, Metadatum> DomainsById { get; set; } = [];

    public void AddMetadatum(string type, Metadatum m)
    {
        switch (type)
        {
            case "acceptLabel":
                AcceptLabels.Add(m);
                AcceptLabelsById[m.Id] = m;
                break;

            case "inflectLabel":
                InflectLabels.Add(m);
                InflectLabelsById[m.Id] = m;
                break;

            case "posLabel":
                PosLabels.Add(m);
                PosLabelsById[m.Id] = m;
                break;

            case "domain":
                Domains.Add(m);
                DomainsById[m.Id] = m;
                break;
        }
    }
}

/// <summary>Represents the names (in Irish and English) and abbreviation of a language.</summary>
public class Language
{
    public string Abbr { get; set; } = "";
    public Dictionary<string, string> Name { get; set; } = [];
    public string Role { get; set; } = "";

    public Language(JObject jo)
    {
        Abbr = (string?)jo["abbr"] ?? "";
        Name["ga"] = (string?)jo["title"]?["ga"] ?? "";
        Name["en"] = (string?)jo["title"]?["en"] ?? "";
        Role = (string?)jo["role"] ?? "";
    }
}

/// <summary>
	/// Represents the names (in Irish and English), the numerical ID, and optionally an abbreviation, of a metadata item.
	/// </summary>
public class Metadatum
{
    public int Id { get; set; }
    public string Abbr { get; set; } = "";
    public Dictionary<string, string> Name { get; set; } = [];
    public int Level { get; set; }
    public string IsFor { get; set; } = "";
    public int ParentID { get; set; }
    public bool HasChildren { get; set; }
    public JObject Jo { get; set; }
    public string SubdomainsJson { get; set; } = "";

    public Metadatum(int id, JObject jo, bool hasChildren)
    {
        Id = id;
        Jo = jo;
        HasChildren = hasChildren;

        Abbr = (string?)jo["abbr"] ?? "";
        Name["ga"] = (string?)jo["title"]?["ga"] ?? "";
        Name["en"] = (string?)jo["title"]?["en"] ?? "";

        if (int.TryParse((string?)jo["level"], out var lvl)) Level = lvl;
        if (int.TryParse((string?)jo["parentID"], out var pid)) ParentID = pid;
    }

    public Metadatum(int id, JObject jo, bool hasChildren, List<Language> langs)
        : this(id, jo, hasChildren)
    {
        if (jo["isfor"] is not JArray arr) return;

        foreach (var s in arr.Values<string>())
        {
            switch (s)
            {
                case "_all":
                    IsFor += ";0;";
                    foreach (var lang in langs)
                        IsFor += $";{lang.Abbr};";
                    break;

                case "_allmajor":
                    foreach (var lang in langs.Where(l => l.Role == "major"))
                        IsFor += $";{lang.Abbr};";
                    break;

                case "_allminor":
                    foreach (var lang in langs.Where(l => l.Role == "minor"))
                        IsFor += $";{lang.Abbr};";
                    break;

                default:
                    IsFor += $";{s};";
                    break;
            }
        }
    }
}

/// <summary>Represents the content of the home page.</summary>
public class Index
{
    public List<DomainListing> Domains { get; set; } = [];

    public string Tod { get; set; } = "";

    public List<string> Recent { get; set; } = [];

    public string NewsGA { get; set; } = "";
    public string NewsEN { get; set; } = "";
}

/// <summary>Represents the content of the single-entry page.</summary>
public class Entry
{
    public int Id { get; set; }

    public string EntryHtml { get; set; } = "";
}

/// <summary>Represents the contents of the quick search page.</summary>
public class QuickSearch
{
    public string Word { get; set; } = "";
    public string Lang { get; set; } = "";
    public bool Super { get; set; }

    public List<string> Similars { get; set; } = [];
    public List<string> Exacts { get; set; } = [];
    public List<string> Relateds { get; set; } = [];

    public bool RelatedMore { get; set; }

    public List<Language> Langs { get; set; } = [];

    public string SortLang { get; set; } = "";

    public Dictionary<string, List<Tuple<string, string>>> Auxes { get; set; } = [];

    public string AdvSearchUrl()
    {
        var encoded = Uri.EscapeDataString(Tools.SlashEncode(Word));
        var langPart = string.IsNullOrEmpty(Lang) ? "0" : Lang;

        return $"/plus/{encoded}/al/ft/lang{langPart}/pos0/dom0/";
    }

    public string IateSearchUrl()
    {
        return $"/iate/{Uri.EscapeDataString(Tools.SlashEncode(Word))}/";
    }

    public string SearchData() => JsonConvert.SerializeObject(new
    {
        word = Word,
        lang = Lang,
        similarsCount = Similars.Count,
        relatedsCount = Relateds.Count,
        RelatedMore,
        langsCount = Langs.Count,
        sortlang = SortLang
    });
}

/// <summary>Represents the contents of a pager.</summary>
public class Pager
{
    public bool Needed { get; set; }

    public int PrevNum { get; set; }
    public List<int> StartNums { get; set; } = [];
    public bool PreDots { get; set; }
    public List<int> PreNums { get; set; } = [];
    public int CurrentNum { get; set; }
    public List<int> PostNums { get; set; } = [];
    public bool PostDots { get; set; }
    public List<int> EndNums { get; set; } = [];
    public int NextNum { get; set; }

    public Pager(int currentPage, int maxPage)
    {
        if (maxPage <= 1)
            return;

        Needed = true;
        CurrentNum = currentPage;

        if (currentPage > 1)
            PrevNum = currentPage - 1;

        if (currentPage < maxPage)
            NextNum = currentPage + 1;

        if (currentPage <= 6)
        {
            for (int i = 1; i < currentPage; i++)
                StartNums.Add(i);
        }
        else
        {
            StartNums.Add(1);
            StartNums.Add(2);
            PreDots = true;

            for (int i = currentPage - 2; i < currentPage; i++)
                PreNums.Add(i);
        }

        if (currentPage >= maxPage - 6)
        {
            for (int i = currentPage + 1; i <= maxPage; i++)
                EndNums.Add(i);
        }
        else
        {
            for (int i = currentPage + 1; i <= currentPage + 2; i++)
                PostNums.Add(i);

            PostDots = true;

            EndNums.Add(maxPage - 1);
            EndNums.Add(maxPage);
        }
    }
}

/// <summary>Represents the contents of the advanced search page.</summary>
public class AdvSearch
{
    public List<Language> Langs { get; set; } = [];
    public List<Metadatum> PosLabels { get; set; } = [];
    public List<Metadatum> Domains { get; set; } = [];

    public string Word { get; set; } = "";
    public string Length { get; set; } = "";
    public string Extent { get; set; } = "";
    public string Lang { get; set; } = "";

    public int PosLabel { get; set; }
    public int DomainID { get; set; }
    public int Page { get; set; }

    public List<string> Matches { get; set; } = new();
    public int Total { get; set; }

    public Pager? Pager { get; set; }

    public string SortLang { get; set; } = "";

    public string UrlByPage(int page)
    {
        var encoded = Tools.SlashEncode(Word);
        var langPart = string.IsNullOrEmpty(Lang) ? "0" : Lang;

        return $"/plus/{encoded}/{Length}/{Extent}/lang{langPart}/pos{PosLabel}/dom{DomainID}/{page}/";
    }

    public string SearchData() => JsonConvert.SerializeObject(new
    {
        word = Word,
        length = Length,
        extent = Extent,
        lang = Lang,
        posLabel = PosLabel,
        domainID = DomainID,
        page = Page,
        sortlang = SortLang
    });
}

/// <summary>
/// Represents the names (in Irish and English) and numeric ID of a (top-level) domain.
/// </summary>
public class DomainListing
{
    public int Id { get; set; }

    public Dictionary<string, string> Name { get; set; } = [];

    public bool HasChildren { get; set; }

    public DomainListing(int id, string nameGA, string nameEN)
    {
        Id = id;
        Name["ga"] = nameGA;
        Name["en"] = nameEN;
    }

    public DomainListing(int id, string nameGA, string nameEN, bool hasChildren)
        : this(id, nameGA, nameEN)
    {
        HasChildren = hasChildren;
    }

    public string GetExpandableTitle(string lang)
    {
        if (!Name.TryGetValue(lang, out var text) || string.IsNullOrEmpty(text))
            return "";

        var driller = HasChildren ? "<span class='driller'>►</span> " : "";
        return $"{driller}<span class='text'>{text}</span>";
    }

    public string GetExpandedTitle(string lang)
    {
        if (!Name.TryGetValue(lang, out var text) || string.IsNullOrEmpty(text))
            return "";

        var driller = HasChildren ? "<span class='driller'>▼</span> " : "";
        return $"{driller}<span class='text'>{text}</span>";
    }
}

/// <summary>Represents the contents of the page that lists all top-level domains.</summary>
public class Domains
{
    public string Lang { get; set; } = "";

    public string LeftLang() => Lang;

    public string RightLang() => Lang == "ga" ? "en" : "ga";

    [JsonProperty("domains")]
    public List<DomainListing> DomainsList { get; set; } = [];
}

/// <summary>Represents the names (in Irish and English) and numeric ID of a subdomain.</summary>
public class SubdomainListing
{
    public int Id { get; set; }

    public Dictionary<string, string> Name { get; set; } = [];

    public int Level { get; set; }

    public bool Visible { get; set; }

    public SubdomainListing? Parent { get; set; }

    public SubdomainListing(int id, string nameGA, string nameEN, int level, bool visible)
    {
        Id = id;
        Name["ga"] = !string.IsNullOrEmpty(nameGA) ? nameGA : nameEN;
        Name["en"] = !string.IsNullOrEmpty(nameEN) ? nameEN : nameGA;
        Level = level;
        Visible = visible;
    }
}

/// <summary>
/// Represents the contents of the page that lists one top-level domain, its subdomains, and some entries.
/// </summary>
public class Domain
{
    public string Lang { get; set; } = "";

    public string LeftLang() => Lang;

    public string RightLang() => Lang == "ga" ? "en" : "ga";

    public int DomID { get; set; }

    public DomainListing? DomainListing { get; set; }

    public List<DomainListing> Parents { get; set; } = [];

    public List<DomainListing> Subdomains { get; set; } = [];

    public int Page { get; set; }

    public List<string> Matches { get; set; } = [];

    public int Total { get; set; }

    public Pager? Pager { get; set; }

    public string UrlByPage(int page)
    {
        return $"/dom/{DomID}/{Lang}/{page}/";
    }

    public string UrlByLang(string lang)
    {
        return $"/dom/{DomID}/{lang}/";
    }

    public string SearchData() => JsonConvert.SerializeObject(new
    {
        domID = DomID,
        page = Page,
        lang = Lang
    });
}
