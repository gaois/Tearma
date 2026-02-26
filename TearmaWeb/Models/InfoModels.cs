namespace TearmaWeb.Models.Info;

/// <summary>Represents the contents of the download page.</summary>
public class Download {}

/// <summary>Represents the contents of the widgets page.</summary>
public class Widgets {}

/// <summary>Represents the contents of one info page.</summary>
public class Topic
{
    public string Section { get; set; } = "";   // eolas | help
    public string Nickname { get; set; } = "";  // <nickname>.<lang>.md
    public string Lang { get; set; } = "";      // ga | en

    public List<TocItem> Toc { get; set; } = [];

    public string Body { get; set; } = "";
}

public class TocItem
{
    public string Nickname { get; set; }
    public Dictionary<string, string> Title { get; set; } = [];

    public TocItem(string nickname, string titleGA, string titleEN)
    {
        Nickname = nickname;
        Title["ga"] = titleGA;
        Title["en"] = titleEN;
    }
}

public static class Toc
{
    public static List<TocItem> InfoToc()
    {
        return
        [
            new("tionscadal", "Tionscadal téarma.ie", "The téarma.ie project"),
            new("stair", "Stair téarma.ie", "History of téarma.ie"),
            new("abhar", "Eolas faoin ábhar", "About the content"),
            new("coiste", "An Coiste Téarmaíochta", "Terminology Committee"),
            new("cosaint-sonrai", "Eolas cosanta sonraí", "Data protection information")
        ];
    }

    public static List<TocItem> HelpToc()
    {
        return
        [
            new("conas-usaid", "Conas an suíomh a úsáid", "How to use the site"),
            new("cad-is-tearma", "Cad is téarma ann?", "What is a term?"),
            new("cuardach-tapa", "Cuardach tapa", "Quick search"),
            new("cuardach-casta", "Cuardach casta", "Advanced search"),
            new("brabhsail", "Brabhsáil", "Browse"),
            new("torthai-a-thuiscint", "Conas na torthaí cuardaigh a thuiscint", "Understanding search results"),
            new("gan-toradh", "Níor aimsigh mé a raibh uaim", "I didn’t find what I was looking for")
        ];
    }
}