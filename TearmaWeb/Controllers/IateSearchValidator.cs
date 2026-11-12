using System.Text.RegularExpressions;

namespace TearmaWeb.Controllers;

public static partial class IateSearchValidator
{
    private static readonly Regex EmailRegex = EmailAddressPattern();

    private static readonly Regex UrlRegex = UrlPattern();

    /// <summary>
    /// Normalizes a search term before cache lookup/storage.
    /// </summary>
    public static string NormalizeCacheKey(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        return word
            .Trim();
    }

    /// <summary>
    /// Returns true if the term is worth querying against IATE.
    /// </summary>
    public static bool ShouldQueryIate(string? word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        word = word.Trim();

        int score = CalculateSpamScore(word);

        return score < 5;
    }

    /// <summary>
    /// Exposed separately for diagnostics/logging.
    /// </summary>
    public static int CalculateSpamScore(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return int.MaxValue;

        int score = 0;

        // Email addresses
        if (EmailRegex.IsMatch(word))
            score += 5;

        // URLs / domains
        if (UrlRegex.IsMatch(word))
            score += 4;

        // User handles etc.
        if (word.Contains('@'))
            score += 2;

        // Extremely long text
        // (long institutional names should still pass)
        if (word.Length > 200)
            score += 2;

        if (word.Length > 400)
            score += 4;

        // Suspicious symbol density
        int symbolCount = word.Count(c =>
            !char.IsLetterOrDigit(c)
                && !char.IsWhiteSpace(c)
                && c != '-'
                && c != '\''
                && c != '.'
                && c != ','
                && c != '('
                && c != ')');

        if (symbolCount > 10)
            score += 2;

        if (symbolCount > 25)
            score += 4;

        // Very non-linguistic strings / Nonsense
        int letters = word.Count(char.IsLetter);

        double letterRatio = letters / (double)Math.Max(word.Length, 1);

        if (letterRatio < 0.25)
            score += 2;

        // Repeated crypto-spam style brackets
        if (word.Contains('【') || word.Contains('】'))
            score += 3;

        return score;
    }

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-IE")]
    private static partial Regex EmailAddressPattern();

    [GeneratedRegex(@"(?:https?://|www\.|[a-z0-9-]+\.(?:com|org|net|xyz|shop|top|ru|cn)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-IE")]
    private static partial Regex UrlPattern();
}
