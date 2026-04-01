using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using TearmaWeb.Controllers.Scripts;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers;

public class Broker(IConfiguration configuration, IMemoryCache cache)
{
    private readonly string _connectionString = 
        configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string.");

    // ---------------------------
    // Shared helpers
    // ---------------------------
    private async Task<Lookups> GetCachedMetadataAsync(
        string metadataSql,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        // Build a unique cache key based on SQL + parameters
        var sb = new StringBuilder(metadataSql);

        if (parameters != null)
            foreach (var kvp in parameters)
                sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);

        var keyBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = Convert.ToHexString(SHA256.HashData(keyBytes));

        string cacheKey = "metadata:" + hash;

        return (await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(metadataSql, conn)
            {
                CommandType = CommandType.Text
            };

            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    cmd.Parameters.AddWithValue("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            return await ReadLookupsAsync(reader);
        }))!;
    }

    private static async Task<Lookups> ReadLookupsAsync(SqlDataReader reader)
    {
        var lookups = new Lookups();

        // Languages
        if (await reader.ReadAsync())
        {
            var json = (string)reader["json"];

            var jo = JObject.Parse(json);
            var ja = (JArray?)jo["languages"] ?? [];

            foreach (var token in ja)
            {
                if (token is JObject jlang)
                {
                    var language = new Language(jlang);
                    lookups.AddLanguage(language);
                }
            }
        }

        // Metadata
        await reader.NextResultAsync();

        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string type = (string)reader["type"];
            string json = reader["json"] as string ?? "";
            bool hasChildren = (int)reader["hasChildren"] > 0;

            var jo = JObject.Parse(json);

            Metadatum metadatum =
                type == "posLabel"
                    ? new Metadatum(id, jo, hasChildren, lookups.Languages)
                    : new Metadatum(id, jo, hasChildren);

            lookups.AddMetadatum(type, metadatum);
        }

        return lookups;
    }

    private static async Task<Dictionary<int, string>> ReadXrefTargetsAsync(SqlDataReader reader)
    {
        var dict = new Dictionary<int, string>();

        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            if (!dict.ContainsKey(id))
            {
                dict[id] = (string)reader["json"];
            }
        }

        return dict;
    }

    // ---------------------------
    // Quick Search
    // ---------------------------

    public async Task DoQuickSearchAsync(QuickSearch model)
    {
        // metadata
        var metadataSql = SqlScripts.Get("pub_tod_metadata.sql");
        var lookups = await GetCachedMetadataAsync(metadataSql);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_quicksearch", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@word", SqlDbType.NVarChar, 255).Value = model.Word;
        command.Parameters.Add("@lang", SqlDbType.NVarChar, 10).Value = model.Lang;
        command.Parameters.Add("@super", SqlDbType.Bit).Value = model.Super;

        await using var reader = await command.ExecuteReaderAsync();

        // Similars
        while (await reader.ReadAsync())
        {
            model.Similars.Add((string)reader["similar"]);
        }

        // Languages
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            string abbr = (string)reader["lang"];
            if (lookups.LanguagesByAbbr.TryGetValue(abbr, out var lang))
                model.Langs.Add(lang);
        }

        // Sorting language
        model.SortLang = model.Lang;
        if (string.IsNullOrEmpty(model.SortLang) && model.Langs.Count > 0)
        {
            model.SortLang = model.Langs[0].Abbr;
            if (model.SortLang != "ga" && model.SortLang != "en")
                model.SortLang = "ga";
        }

        // Xref targets
        await reader.NextResultAsync();
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Exact matches
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.Exacts.Add(Prettify.Entry(id, json, lookups, model.SortLang, xrefTargets));
        }

        // Related matches
        await reader.NextResultAsync();
        int relatedCount = 0;

        while (await reader.ReadAsync())
        {
            relatedCount++;
            if (relatedCount <= 100)
            {
                int id = (int)reader["id"];
                string json = reader["json"] as string ?? "";
                model.Relateds.Add(Prettify.Entry(id, json, lookups, model.SortLang));
            }
        }

        if (relatedCount > 100)
            model.RelatedMore = true;

        // Aux matches
        if (model.Super)
        {
            await reader.NextResultAsync();

            while (await reader.ReadAsync())
            {
                var coll = (string)reader["coll"];

                if (!model.Auxes.ContainsKey(coll))
                    model.Auxes[coll] = [];

                model.Auxes[coll].Add(
                    new Tuple<string, string>(
                        (string)reader["en"],
                        (string)reader["ga"]
                    )
                );
            }
        }
    }

    public async Task PrepareAdvSearchAsync(AdvSearch model)
    {
        var metadataSql = SqlScripts.Get("pub_advsearch_prepare.sql");
        var lookups = await GetCachedMetadataAsync(metadataSql);

        foreach (var language in lookups.Languages)
            model.Langs.Add(language);

        foreach (var datum in lookups.PosLabels)
            model.PosLabels.Add(datum);

        foreach (var datum in lookups.Domains)
            model.Domains.Add(datum);
    }

    public async Task DoAdvSearchAsync(AdvSearch model)
    {
        //------------------------------------------------------------------
        // Load metadata from cache
        //------------------------------------------------------------------
        var metadataSql = SqlScripts.Get("pub_advsearch_metadata.sql");
        var lookups = await GetCachedMetadataAsync(metadataSql);

        foreach (var language in lookups.Languages)
            model.Langs.Add(language);

        foreach (var datum in lookups.PosLabels)
            model.PosLabels.Add(datum);

        foreach (var datum in lookups.Domains)
            model.Domains.Add(datum);

        //------------------------------------------------------------------
        // Generate dynamic SQL for query
        //------------------------------------------------------------------
        var sql = SqlScripts.Get("pub_advsearch.sql");

        //------------------------------------------------------------------
        // Build SELECT clause (language-specific ordering)
        //------------------------------------------------------------------
        var selectClause =
            model.Lang == "en"
            ? "select distinct ret.id, row_number() over (order by ret.sortkeyen)"
            : "select distinct ret.id, row_number() over (order by ret.sortkeyga)";

        sql = sql.Replace("/**select**/", selectClause);

        //------------------------------------------------------------------
        // Build first WHERE clause dynamically
        //------------------------------------------------------------------
        var where1 = new List<string>
        {
            "(e.pStatus = 1)"
        };

        // language
        if (!string.IsNullOrEmpty(model.Lang))
            where1.Add("(t.lang = @lang)");

        // pos
        if (model.PosLabel != 0)
            where1.Add("(tp.pos_id = @pos)");

        // domain
        if (model.DomainID != 0)
            where1.Add("(ed.superdomain in (select domainid from expanddomainid_inline(@dom, default)))");

        // length
        switch (model.Length)
        {
            case "sw": where1.Add("(t.wording not like '% %')"); break;
            case "mw": where1.Add("(t.wording like '% %')"); break;
        }

        // extent
        switch (model.Extent)
        {
            case "st": where1.Add("(t.wording like @word + '%' escape '\\')"); break;
            case "ed": where1.Add("(t.wording_rev like reverse(@word) + '%' escape '\\')"); break;
            case "pt": where1.Add("(t.wording like '%' + @word + '%' escape '\\')"); break;
            case "md": where1.Add("(t.wording like '_%' + @word + '%_' escape '\\')"); break;
            case "al": where1.Add("(t.wording like @word escape '\\')"); break;
        }

        var where1Clause = where1.Count > 0
            ? "where " + string.Join(" and ", where1)
            : "";

        sql = sql.Replace("/**where1**/", where1Clause);

        //------------------------------------------------------------------
        // Build second WHERE clause dynamically
        //------------------------------------------------------------------
        var where2 = new List<string>();

        // language
        if (!string.IsNullOrEmpty(model.Lang))
            where2.Add("(t.lang = @lang)");

        // length
        switch (model.Length)
        {
            case "sw": where2.Add("(t.wording not like '% %')"); break;
            case "mw": where2.Add("(t.wording like '% %')"); break;
        }

        var where2Clause = where2.Count > 0
            ? "where " + string.Join(" and ", where2)
            : "";

        sql = sql.Replace("/**where2**/", where2Clause);

        //------------------------------------------------------------------
        // Build third WHERE clause dynamically
        //------------------------------------------------------------------
        var where3 = new List<string>
        {
            "(temp.term_id = t.id)"
        };

        // language
        if (!string.IsNullOrEmpty(model.Lang))
            where3.Add("(t.lang = @lang)");

        var where3Clause = where3.Count > 0
            ? "where " + string.Join(" and ", where3)
            : "";

        sql = sql.Replace("/**where3**/", where3Clause);

        //------------------------------------------------------------------
        // Build third WHERE clause dynamically
        //------------------------------------------------------------------
        var where4 = new List<string>();

        // pos
        if (model.PosLabel != 0)
            where4.Add("(tp.pos_id = @pos)");

        // domain
        if (model.DomainID != 0)
            where4.Add("(ed.superdomain in (select domainid from expanddomainid_inline(@dom, default)))");

        var where4Clause = where4.Count > 0
            ? "where " + string.Join(" and ", where4)
            : "";

        sql = sql.Replace("/**where4**/", where4Clause);

        //------------------------------------------------------------------
        // Execute and read query results
        //------------------------------------------------------------------
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand(sql, conn)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.Add("@word", SqlDbType.NVarChar, 255).Value = model.Word;
        command.Parameters.Add("@length", SqlDbType.NVarChar, 2).Value = model.Length;
        command.Parameters.Add("@extent", SqlDbType.NVarChar, 2).Value = model.Extent;
        command.Parameters.Add("@lang", SqlDbType.NVarChar, 255).Value = model.Lang;
        command.Parameters.Add("@pos", SqlDbType.Int).Value = model.PosLabel;
        command.Parameters.Add("@dom", SqlDbType.Int).Value = model.DomainID;
        command.Parameters.Add("@page", SqlDbType.Int).Value = model.Page;

        var totalParam = new SqlParameter("@total", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value = model.Total
        };

        command.Parameters.Add(totalParam);

        await using var reader = await command.ExecuteReaderAsync();

        // Sorting language
        model.SortLang = model.Lang;
        if (model.SortLang != "ga" && model.SortLang != "en")
            model.SortLang = "ga";

        // Xref targets
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Matches
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.Matches.Add(Prettify.Entry(id, json, lookups, model.SortLang, xrefTargets));
        }

        // Pager
        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            int currentPage = (int)reader["currentPage"];
            int maxPage = (int)reader["maxPage"];
            model.Pager = new Pager(currentPage, maxPage);
        }

        await reader.DisposeAsync();
        model.Total = (int)totalParam.Value;
    }

    public async Task DoDomainsAsync(Domains model)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_domains", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@lang", SqlDbType.NVarChar, 255).Value = model.Lang;

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        foreach (var md in lookups.Domains)
        {
            if (md.ParentID == 0)
            {
                model.DomainsList.Add(
                    new DomainListing(md.Id, md.Name["ga"], md.Name["en"], md.HasChildren)
                );
            }
        }
    }

    public async Task DoIndexAsync(Models.Home.Index model)
    {
        // Metadata
        var metadataSql = SqlScripts.Get("pub_quicksearch_metadata.sql");
        var lookups = await GetCachedMetadataAsync(metadataSql);

        // Main query
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_index", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync();

        // Domains
        foreach (var md in lookups.Domains)
        {
            if (md.ParentID == 0)
                model.Domains.Add(new DomainListing(md.Id, md.Name["ga"], md.Name["en"]));
        }

        // Term of the day
        if (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.Tod = Prettify.Entry(id, json, lookups, "ga");
        }

        // Recent entries
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.Recent.Add(Prettify.EntryLink(id, json, "ga"));
        }

        // News
        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            model.NewsGA = (string)reader["TextGA"];
            model.NewsEN = (string)reader["TextEN"];
        }
    }

    public async Task DoEntryAsync(Entry model)
    {
        // Metadata
        var metadataSql = SqlScripts.Get("pub_quicksearch_metadata.sql");
        var lookups = await GetCachedMetadataAsync(metadataSql);

        // Main query
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_entry", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@id", SqlDbType.Int).Value = model.Id;

        await using var reader = await command.ExecuteReaderAsync();

        // Xref targets
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Entry
        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.EntryHtml = Prettify.Entry(id, json, lookups, "ga", xrefTargets);
        }
    }

    public async Task DoDomainAsync(Domain model)
    {
        // Metadata
        var metadataSql = SqlScripts.Get("pub_domain_metadata.sql");
        var lookups = await GetCachedMetadataAsync(
            metadataSql,
            new Dictionary<string, object?> { ["lang"] = model.Lang });

        // Main query
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_domain", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@lang", SqlDbType.NVarChar, 255).Value = model.Lang;
        command.Parameters.Add("@domID", SqlDbType.Int).Value = model.DomID;
        command.Parameters.Add("@page", SqlDbType.Int).Value = model.Page;

        var totalParam = new SqlParameter("@total", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value = 0
        };

        command.Parameters.Add(totalParam);

        await using var reader = await command.ExecuteReaderAsync();

        // Domain + parents + children
        if (lookups.DomainsById.TryGetValue(model.DomID, out var md))
        {
            model.DomainListing = new DomainListing(md.Id, md.Name["ga"], md.Name["en"], md.HasChildren);

            // Parents
            while (lookups.DomainsById.TryGetValue(md.ParentID, out var parent) && parent.ParentID != 0)
            {
                model.Parents.Add(
                    new DomainListing(
                        parent.Id, parent.Name["ga"], parent.Name["en"], parent.HasChildren
                    )
                );
                md = parent;
            }

            model.Parents.Reverse();

            // Subdomains
            foreach (var smd in lookups.Domains)
            {
                if (smd.ParentID == model.DomID)
                {
                    model.Subdomains.Add(new DomainListing(smd.Id, smd.Name["ga"], smd.Name["en"], smd.HasChildren));
                }
            }
        }

        // Xref targets
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Matches
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.Matches.Add(Prettify.Entry(id, json, lookups, model.Lang, xrefTargets));
        }

        // Pager
        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            int currentPage = (int)reader["currentPage"];
            int maxPage = (int)reader["maxPage"];
            model.Pager = new Pager(currentPage, maxPage);
        }

        await reader.DisposeAsync();
        model.Total = (int)totalParam.Value;
    }

    public async Task DoTodAsync(Models.Widgets.Tod model)
    {
        var metadataSql = SqlScripts.Get("pub_tod_metadata.sql");
        var lookups = await GetCachedMetadataAsync(metadataSql);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_tod", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = reader["json"] as string ?? "";
            model.TodID = id;
            model.TodText = Prettify.Entry(id, json, lookups, "ga");
        }
    }
}
