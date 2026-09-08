using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TearmaWeb.Models;
using TearmaWeb.Models.Iate;
using System.Data;
using Microsoft.Data.SqlClient;

namespace TearmaWeb.Controllers;

public class IateBroker(IConfiguration config, IHttpClientFactory httpClientFactory)
{
    private readonly string _iateUsername = config["IATE:Username"]!;
    private readonly string _iatePassword = config["IATE:Password"]!;

    private readonly string _connectionString = 
        config.GetConnectionString("IateCacheConnection")
            ?? throw new InvalidOperationException("Missing connection string.");

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
        IateMultiSearchResponse? json;
        
        string cached = await TryGetFromCacheAsync(model.Word);
        if(cached != "")
        {
            json = JsonSerializer.Deserialize<IateMultiSearchResponse>(cached, JsonOptions);
        }
        else
        {
            var token = await GetAccessTokenAsync();
            var client = httpClientFactory.CreateClient("IATE");

            var payload = IateSearchPayloadBuilder.Build(model.Word);
            using var request = BuildSearchRequest(token, payload);

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadFromJsonAsync<IateMultiSearchResponse>(JsonOptions);
            _ = SaveToCacheAsync(model.Word, JsonSerializer.Serialize(json));
        }
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
        IateMultiSearchResponse? json;

        string cached = await TryGetFromCacheAsync(model.Word);
        if(cached != "")
        {
            json = JsonSerializer.Deserialize<IateMultiSearchResponse>(cached, JsonOptions);
        }
        else
        {
            var token = await GetAccessTokenAsync();
            var client = httpClientFactory.CreateClient("IATE");

            var payload = IateSearchPayloadBuilder.Build(model.Word);
            using var request = BuildSearchRequest(token, payload);

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadFromJsonAsync<IateMultiSearchResponse>(JsonOptions);
            if (json == null) return;
            _ = SaveToCacheAsync(model.Word, JsonSerializer.Serialize(json));
        }
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

    private async Task SaveToCacheAsync(string word, string payload)
    {
        //Console.WriteLine("saving into iate cache: "+ word);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("insert into tblCache(Word, Payload) values(@word, @payload)", conn)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.Add("@word", SqlDbType.NVarChar, 255).Value = word;
        command.Parameters.Add("@payload", SqlDbType.NVarChar).Value = payload;

        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> TryGetFromCacheAsync(string word)
    {
        //Console.WriteLine("trying to get from iate cache: "+ word);

        string payload = "";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("select top 1 Payload from tblCache where Word = @word", conn)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.Add("@word", SqlDbType.NVarChar, 255).Value = word;

        await using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            payload = (string)reader["Payload"];
        }

        //if(payload != "")
        //{
        //    Console.WriteLine("found in iate cache: "+ word);
        //} else
        //{
        //    Console.WriteLine("not found in iate cache: "+ word);
        //}

        return payload;
    }
}
