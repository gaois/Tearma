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
                $"uac-api/oauth2/token?grant_type=password&username={Uri.EscapeDataString(_iateUsername)}&password={Uri.EscapeDataString(_iatePassword)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("accept", "application/vnd.iate.token+json; version=2");
            request.Content = new StringContent("application/x-www-form-urlencoded");

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<IateTokenResponse>(JsonOptions);
            var token = json?.Tokens?.FirstOrDefault()?.AccessToken
                ?? throw new InvalidOperationException("No access token returned.");

            // Cache token + expiry
            _cachedToken = token;

            // IATE tokens typically last hours; if not provided, assume 1 hour
            _tokenExpiry = DateTimeOffset.UtcNow.AddHours(1);

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

        var payload = BuildSearchPayload(model.Word);
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

        var payload = BuildSearchPayload(model.Word);
        using var request = BuildSearchRequest(token, payload);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<IateMultiSearchResponse>(JsonOptions);
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
            "em-api/entries/_msearch?fields_set_name=minimal");

        request.Headers.Add("accept", "application/vnd.iate.entry+json; version=2");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token);
        request.Content = JsonContent.Create(payload);

        return request;
    }

    private static object BuildSearchPayload(string word) => new[]
    {
        new
        {
            limit = 10,
            expand = true,
            search_request = new
            {
                sources = new[]{"ga"},
                targets = new[]{"en", "de", "fr"},
                query = word,
                query_operator = 3
            }
        },
        new
        {
            limit = 10,
            expand = true,
            search_request = new
            {
                sources = new[]{"en"},
                targets = new[]{"ga", "de", "fr"},
                query = word,
                query_operator = 3
            }
        },
        new
        {
            limit = 101,
            expand = true,
            search_request = new
            {
                sources = new[]{"ga"},
                targets = new[]{"en", "de", "fr"},
                query = word,
                query_operator = 1
            }
        },
        new
        {
            limit = 101,
            expand = true,
            search_request = new
            {
                sources = new[]{"en"},
                targets = new[]{"ga", "de", "fr"},
                query = word,
                query_operator = 1
            }
        }
    };
}
