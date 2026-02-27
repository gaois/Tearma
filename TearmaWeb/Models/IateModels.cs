using System.Text.Json.Serialization;

namespace TearmaWeb.Models.Iate;

public class Tools
{
	public static string SlashEncode(string text)
	{
		text = text.Replace(@"%", "%25");
		text = text.Replace(@"\", "$backslash;");
		text = text.Replace(@"/", "$forwardslash;");
		return text;
	}
}

public class IateContext
{
    [JsonPropertyName("context")]
    public string? Context { get; set; }
}

public class IateDefinition
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class IateDomain
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("path")]
    public List<string>? Path { get; set; }
}

public class IateDomainWrapper
{
    [JsonPropertyName("domain")]
    public IateDomain? Domain { get; set; }
}

public class IateEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("self")]
    public IateSelfLink? Self { get; set; }

    [JsonPropertyName("domains")]
    public List<IateDomainWrapper>? Domains { get; set; }

    [JsonPropertyName("language")]
    public Dictionary<string, IateLanguageBlock>? Language { get; set; }
}

public class IateLanguageBlock
{
    [JsonPropertyName("term_entries")]
    public List<IateTermEntry>? TermEntries { get; set; }

    [JsonPropertyName("definition")]
    public IateDefinition? Definition { get; set; }
}

public class IateMultiSearchResponse
{
    [JsonPropertyName("responses")]
    public List<IateSearchBlock> Responses { get; set; } = [];
}

public class IateSearchBlock
{
    [JsonPropertyName("items")]
    public List<IateEntry>? Items { get; set; }
}

public class IateSearchRequestBlock
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("expand")]
    public bool Expand { get; set; }

    [JsonPropertyName("search_request")]
    public IateSearchRequest SearchRequest { get; set; } = default!;
}

public class IateSearchRequest
{
    [JsonPropertyName("sources")]
    public string[] Sources { get; set; } = [];

    [JsonPropertyName("targets")]
    public string[] Targets { get; set; } = [];

    [JsonPropertyName("query")]
    public string Query { get; set; } = "";

    [JsonPropertyName("query_operator")]
    public int QueryOperator { get; set; }
}

public class IateSelfLink
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

public class IateTermEntry
{
    [JsonPropertyName("term_value")]
    public string? TermValue { get; set; }

    [JsonPropertyName("contexts")]
    public List<IateContext>? Contexts { get; set; }
}

public class IateToken
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}

public class IateTokenResponse
{
    [JsonPropertyName("tokens")]
    public List<IateToken>? Tokens { get; set; }
}

/// <summary>Represents the contents of the IATE search page.</summary>
public class Search
{
	/// <summary>The string the user has typed into the search box.</summary>
	public string Word = "";

	/// <summary>
	/// The language code of the language in which the user has requested to see results. Empty string if all languages.
	/// </summary>
	public string Lang = "";

	/// <summary>Whether this search is in superser mode (with the auxilliary glossary etc).</summary>
	public bool Super = false;

	public string QuickSearchUrl()
	{
		return $"/q/{Uri.EscapeDataString(Tools.SlashEncode(Word))}/";
	}

	public int Count = 0;
	public bool HasMore = false;
	public List<string> Exacts = [];
	public List<string> Relateds = [];
}