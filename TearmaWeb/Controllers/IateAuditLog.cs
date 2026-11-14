using System.Text;

namespace TearmaWeb.Controllers;

public static class IateAuditLog
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private static readonly string LogDirectory =
        Path.Combine(AppContext.BaseDirectory, "logs", "iate");

    public static async Task LogAsync(
        string endpoint,
        string word,
        string apiUrl,
        string userAgent,
        string ipAddress,
        string? referrer)
    {
        await Lock.WaitAsync();

        try
        {
            Directory.CreateDirectory(LogDirectory);

            string filePath = GetLogFilePath();

            bool fileExists = File.Exists(filePath);

            await using var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            await using var writer = new StreamWriter(stream, Encoding.UTF8);

            if (!fileExists)
            {
                await writer.WriteLineAsync(
                    "\"TimestampUtc\"," +
                    "\"Endpoint\"," +
                    "\"SpamScore\"," +
                    "\"Word\"," +
                    "\"ApiUrl\"," +
                    "\"UserAgent\"," +
                    "\"IPAddress\"," +
                    "\"Referrer\"");
            }

            int spamScore = IateSearchValidator.CalculateSpamScore(word);

            string[] fields =
            [
                DateTime.UtcNow.ToString("O"),
                endpoint,
                spamScore.ToString(),
                word,
                apiUrl,
                userAgent,
                ipAddress,
                referrer ?? string.Empty
            ];

            string line = string.Join(",", fields.Select(CsvEscape));

            await writer.WriteLineAsync(line);
        }
        finally
        {
            Lock.Release();
        }
    }

    private static string GetLogFilePath()
    {
        return Path.Combine(
            LogDirectory,
            $"iate-api-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    private static string CsvEscape(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}