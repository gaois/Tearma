using System.ComponentModel.DataAnnotations;

namespace TearmaWeb.Models.Ask;

public class Ask
{
    // empty | error | thanks
    [Required]
    public string Mode { get; set; } = "empty";

    // Submitted data
    public string? TermEN { get; set; }
    public string? TermXX { get; set; }
    public string? Context { get; set; }
    public string? Def { get; set; }
    public string? Example { get; set; }
    public string? Other { get; set; }
    public string? TermGA { get; set; }
    public string? Name { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    // Captcha transcription submitted by the user
    public string? CaptchaCode { get; set; }
}
