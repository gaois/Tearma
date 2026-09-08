using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TearmaWeb.Models;
using TearmaWeb.Models.Iate;

namespace TearmaWeb.Controllers;

public class IateBroker(IConfiguration config, IHttpClientFactory httpClientFactory)
{
    private readonly string _iateUsername = config["IATE:Username"]!;
    private readonly string _iatePassword = config["IATE:Password"]!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Cached token + expiry
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    // Lock to prevent concurrent refreshes
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private async Task<string> GetAccessTokenAsync()
    {
        // Fast path: token still valid
        if (_cachedToken is not null && _tokenExpiry > DateTimeOffset.UtcNow)
            return _cachedToken;

        await _tokenLock.WaitAsync();

        try
        {
            // Double-check inside lock
            if (_cachedToken is not null && _tokenExpiry > DateTimeOffset.UtcNow)
                return _cachedToken;

            var client = httpClientFactory.CreateClient("IATE");

            var url =
                $"https://iate.europa.eu/uac-api/oauth2/token?grant_type=password&username={Uri.EscapeDataString(_iateUsername)}&password={Uri.EscapeDataString(_iatePassword)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            const string acceptHeader = "application/vnd.iate.token+json; version=2";
            const string mediaTypeHeader = "application/x-www-form-urlencoded";

            request.Headers.Add("accept", acceptHeader);
            request.Content = new StringContent("");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaTypeHeader);

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<IateTokenResponse>(JsonOptions);
            var token = json?.Tokens?.FirstOrDefault()?.AccessToken
                ?? throw new InvalidOperationException("No access token returned.");

            // Cache token + expiry
            _cachedToken = token;

            // IATE tokens typically last 3 hours; we will use 2 to be safe
            _tokenExpiry = DateTimeOffset.UtcNow.AddHours(2);

            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task PeekAsync(PeekResult model)
    {
        var token = await GetAccessTokenAsync();
        var client = httpClientFactory.CreateClient("IATE");

        var payload = IateSearchPayloadBuilder.Build(model.Word);
        using var request = BuildSearchRequest(token, payload);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<IateMultiSearchResponse>(JsonOptions);
        if (json == null) return;

        var urls = new HashSet<string>();

        foreach (var block in json.Responses)
        {
            if (block.Items == null) continue;

            foreach (var entry in block.Items)
            {
                if (model.Count >= 100)
                {
                    model.HasMore = true;
                    return;
                }

                var url = entry.Self?.Href;

                if (url == null || urls.Contains(url)) continue;

                bool hasGA = entry.Language?.TryGetValue("ga", out var ga) == true
                    && ga.TermEntries?.Count > 0;

                bool hasEN = entry.Language?.TryGetValue("en", out var en) == true
                    && en.TermEntries?.Count > 0;

                if (hasGA && hasEN)
                {
                    urls.Add(url);
                    model.Count++;
                }
            }
        }
    }

    public async Task DoSearchAsync(Search model)
    {
        var token = await GetAccessTokenAsync();
        var client = httpClientFactory.CreateClient("IATE");

        var payload = IateSearchPayloadBuilder.Build(model.Word);
        using var request = BuildSearchRequest(token, payload);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadFromJsonAsync<IateMultiSearchResponse>(JsonOptions);
        if (json == null) return;

        var ids = new HashSet<int>();

        for (int i = 0; i < json.Responses.Count; i++)
        {
            var block = json.Responses[i];
            if (block.Items == null) continue;

            foreach (var entry in block.Items)
            {
                if (model.Count >= 100)
                {
                    model.HasMore = true;
                    return;
                }

                if (!ids.Add(entry.Id)) continue;

                bool hasGA = entry.Language?.TryGetValue("ga", out var ga) == true
                    && ga.TermEntries?.Count > 0;

                bool hasEN = entry.Language?.TryGetValue("en", out var en) == true
                    && en.TermEntries?.Count > 0;

                if (!hasGA || !hasEN) continue;

                string left = (i == 0 || i == 2) ? "ga" : "en";
                string right = left == "ga" ? "en" : "ga";

                var pretty = PrettifyIate.Entry(entry, left, right);

                if (i < 2)
                    model.Exacts.Add(pretty);
                else
                    model.Relateds.Add(pretty);

                model.Count++;
            }
        }
    }

    private static HttpRequestMessage BuildSearchRequest(string token, object payload)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://iate.europa.eu/em-api/entries/_msearch?fields_set_name=minimal");

        request.Headers.Add("accept", "application/vnd.iate.entry+json; version=2");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            new MediaTypeHeaderValue("application/json"));

        return request;
    }
}
