using Newtonsoft.Json;

namespace TearmaWeb.Models.Data;

public class Entry
{
    public string? DStatus { get; set; }
    public List<int?> Domains { get; set; } = [];
    public List<Desig> Desigs { get; set; } = [];
    public Dictionary<string, string> Intros { get; set; } = [];
    public List<Definition> Definitions { get; set; } = [];
    public List<Example> Examples { get; set; } = [];
    public List<int> Xrefs { get; set; } = [];
}

public class Example
{
    public Dictionary<string, List<string>> Texts { get; set; } = [];
    public int Nonessential { get; set; }
}

public class Definition
{
    public Dictionary<string, string> Texts { get; set; } = [];
    public List<int?> Domains { get; set; } = [];
    public int Nonessential { get; set; }
}

public class Desig
{
    public Term Term { get; set; } = new();
    public int? Accept { get; set; }
    public string? Clarif { get; set; }
    public int Nonessential { get; set; }
}

public class Term
{
    public string? Lang { get; set; }
    public string? Wording { get; set; }
    public List<Annot> Annots { get; set; } = [];
    public List<Inflect> Inflects { get; set; } = [];
}

public class Annot
{
    [JsonConverter(typeof(Controllers.IntegerJsonConverter))]
    public int Start { get; set; }

    [JsonConverter(typeof(Controllers.IntegerJsonConverter))]
    public int Stop { get; set; }

    public AnnotLabel Label { get; set; } = new();
}

public class AnnotLabel
{
    public string? Type { get; set; }
    public string? Value { get; set; }
}

public class Inflect
{
    public int Label { get; set; }
    public string? Text { get; set; }
}
