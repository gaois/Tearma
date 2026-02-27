namespace TearmaWeb.Models; 

public class PeekResult
{
    public string Word {get; set;} = "";

    public int Count {get; set;} = 0;

    public bool HasMore {get; set;} = false;
}
