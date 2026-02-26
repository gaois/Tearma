using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;
using TearmaWeb.Models.Home;
using System.Data;

namespace TearmaWeb.Controllers;

public class Broker(IConfiguration configuration)
{
    private readonly string _connectionString = 
        configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string.");

    // ---------------------------
    // Shared helpers
    // ---------------------------

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
            string json = (string)reader["json"];
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
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_quicksearch", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@word", model.Word);
        command.Parameters.AddWithValue("@lang", model.Lang);
        command.Parameters.AddWithValue("@super", model.Super);

        await using var reader = await command.ExecuteReaderAsync();

        // Lookups
        var lookups = await ReadLookupsAsync(reader);

        // Similars
        await reader.NextResultAsync();
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
            string json = (string)reader["json"];
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
                string json = (string)reader["json"];
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
                    model.Auxes[coll] = new List<Tuple<string, string>>();

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
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_advsearch_prepare", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        foreach (var language in lookups.Languages)
            model.Langs.Add(language);

        foreach (var datum in lookups.PosLabels)
            model.PosLabels.Add(datum);

        foreach (var datum in lookups.Domains)
            model.Domains.Add(datum);
    }

    public async Task DoAdvSearchAsync(AdvSearch model)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_advsearch", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@word", model.Word);
        command.Parameters.AddWithValue("@length", model.Length);
        command.Parameters.AddWithValue("@extent", model.Extent);
        command.Parameters.AddWithValue("@lang", model.Lang);
        command.Parameters.AddWithValue("@pos", model.PosLabel);
        command.Parameters.AddWithValue("@dom", model.DomainID);
        command.Parameters.AddWithValue("@page", model.Page);

        var totalParam = new SqlParameter("@total", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value = model.Total
        };
        command.Parameters.Add(totalParam);

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        foreach (var language in lookups.Languages)
            model.Langs.Add(language);

        foreach (var datum in lookups.PosLabels)
            model.PosLabels.Add(datum);

        foreach (var datum in lookups.Domains)
            model.Domains.Add(datum);

        // Sorting language
        model.SortLang = model.Lang;
        if (model.SortLang != "ga" && model.SortLang != "en")
            model.SortLang = "ga";

        // Xref targets
        await reader.NextResultAsync();
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Matches
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = (string)reader["json"];
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

        command.Parameters.AddWithValue("@lang", model.Lang);

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
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_index", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        // Domains
        foreach (var md in lookups.Domains)
        {
            if (md.ParentID == 0)
                model.Domains.Add(new DomainListing(md.Id, md.Name["ga"], md.Name["en"]));
        }

        // Term of the day
        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = (string)reader["json"];
            model.Tod = Prettify.Entry(id, json, lookups, "ga");
        }

        // Recent entries
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = (string)reader["json"];
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
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_entry", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@id", model.Id);

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        // Xref targets
        await reader.NextResultAsync();
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Entry
        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = (string)reader["json"];
            model.EntryHtml = Prettify.Entry(id, json, lookups, "ga", xrefTargets);
        }
    }

    public async Task DoDomainAsync(Domain model)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_domain", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@lang", model.Lang);
        command.Parameters.AddWithValue("@domID", model.DomID);
        command.Parameters.AddWithValue("@page", model.Page);

        var totalParam = new SqlParameter("@total", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value = 0
        };
        command.Parameters.Add(totalParam);

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        // Domain + parents + children
        if (lookups.DomainsById.TryGetValue(model.DomID, out var md))
        {
            model.DomainListing = new DomainListing(md.Id, md.Name["ga"], md.Name["en"], md.HasChildren);

            // Parents
            while (lookups.DomainsById.TryGetValue(md.ParentID, out var parent) && parent.ParentID != 0)
            {
                model.Parents.Add(new DomainListing(parent.Id, parent.Name["ga"], parent.Name["en"], parent.HasChildren));
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
        await reader.NextResultAsync();
        var xrefTargets = await ReadXrefTargetsAsync(reader);

        // Matches
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = (string)reader["json"];
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

        model.Total = (int)totalParam.Value;
    }

    public async Task DoTodAsync(Models.Widgets.Tod model)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var command = new SqlCommand("dbo.pub_tod", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync();

        var lookups = await ReadLookupsAsync(reader);

        await reader.NextResultAsync();
        if (await reader.ReadAsync())
        {
            int id = (int)reader["id"];
            string json = (string)reader["json"];
            model.TodID = id;
            model.TodText = Prettify.Entry(id, json, lookups, "ga");
        }
    }
}
