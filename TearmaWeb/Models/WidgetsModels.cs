using Newtonsoft.Json;

namespace TearmaWeb.Models.Widgets;

/// <summary>Represents the contents of the searchbox widget.</summary>
public class Box
{
}

/// <summary>Represents the contents of the 'Term of the day' widget.</summary>
public class Tod
{
    /// <summary>Term of the day.</summary>
    [JsonProperty("tod")]
    public string TodText { get; set; } = "";

    public int TodID { get; set; }
}
