using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace TearmaWeb.Controllers {
	public class Broker {

		private static string GetConnectionString() {
			return @"Server=localhost\sqlexpress;Database=tearma;Trusted_Connection=True;";
		}

		private static Models.Home.Lookups ReadLookups(SqlDataReader reader) {
			Models.Home.Lookups lookups=new Models.Home.Lookups();

			//read lingo config:
			if(reader.Read()) {
				string json=(string)reader["json"];
				JObject jo=JObject.Parse(json);
				JArray ja=(JArray)jo.Property("languages")?.Value ?? new JArray();
				for(int i=0; i<ja.Count; i++) {
					JObject jlang=(JObject)ja[i];
					Models.Home.Language language=new Models.Home.Language(jlang);
					lookups.addLanguage(language);
				}
			}

			//read metadata:
			reader.NextResult();
			while(reader.Read()) {
				int id=(int)reader["id"];
				string type=(string)reader["type"];
				string json=(string)reader["json"];
				JObject jo=JObject.Parse(json);
				Models.Home.Metadatum metadatum=new Models.Home.Metadatum(id, jo);
				lookups.addMetadatatum(type, metadatum);
			}

			return lookups;
		}

		/// <summary>Takes the view model of the quick search page (with 'word' and 'lang' filled in) and populates all other properties from the database.</summary>
		public static void DoQuickSearch(Models.Home.QuickSearch model) {
			SqlConnection conn=new SqlConnection(GetConnectionString());
			conn.Open();
			SqlCommand command=new SqlCommand("dbo.pub_quicksearch", conn);
			command.CommandType=CommandType.StoredProcedure;
			SqlParameter param;
			param=new SqlParameter(); param.ParameterName="@word"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.word; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			SqlDataReader reader=command.ExecuteReader();

			//read lookups:
			Models.Home.Lookups lookups=ReadLookups(reader);

			//read similars:
			reader.NextResult();
			while(reader.Read()) {
				model.similars.Add((string)reader["similar"]);
			}

			//read exact matches:
			reader.NextResult();
			while(reader.Read()) {
				int id=(int)reader["id"];
				string json=(string)reader["json"];
				model.exacts.Add(Prettify.Entry(id, json, lookups));
			}

			//read related matches:
			reader.NextResult();
			int relatedCount=0;
			while(reader.Read()) {
				relatedCount++;
				if(relatedCount <= 100) {
					int id=(int)reader["id"];
					string json=(string)reader["json"];
					model.relateds.Add(Prettify.Entry(id, json, lookups));
				}
			}
			if(relatedCount>100) model.relatedMore=true;

			//read languages in which matches have been found:
			reader.NextResult();
			while(reader.Read()) {
				string abbr=(string)reader["lang"];
				if(lookups.languagesByAbbr.ContainsKey(abbr)) model.langs.Add(lookups.languagesByAbbr[abbr]);
			}

			reader.Close();
			conn.Close();
		}

		/// <summary>Takes the view model of the advanced search page and populates the 'langs' property from the database.</summary>
		public static void PrepareAdvSearch(Models.Home.AdvSearch model) {
			SqlConnection conn=new SqlConnection(GetConnectionString());
			conn.Open();
			SqlCommand command=new SqlCommand("dbo.pub_advsearch_prepare", conn);
			command.CommandType=CommandType.StoredProcedure;
			SqlDataReader reader=command.ExecuteReader();

			//read lookups:
			Models.Home.Lookups lookups=ReadLookups(reader);
			foreach(Models.Home.Language language in lookups.languages) model.langs.Add(language);

			reader.Close();
			conn.Close();
		}

		/// <summary>Takes the view model of the advanced search page (with 'word', 'length', 'extent', 'lang' and 'page' filled in) and populates all other properties from the database.</summary>
		public static void DoAdvSearch(Models.Home.AdvSearch model) {
			SqlConnection conn=new SqlConnection(GetConnectionString());
			conn.Open();
			SqlCommand command=new SqlCommand("dbo.pub_advsearch", conn);
			command.CommandType=CommandType.StoredProcedure;
			SqlParameter param;
			param=new SqlParameter(); param.ParameterName="@word"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.word; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@length"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.length; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@extent"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.extent; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@page"; param.SqlDbType=SqlDbType.Int; param.Value=model.page; command.Parameters.Add(param);
			SqlDataReader reader=command.ExecuteReader();

			//read lookups:
			Models.Home.Lookups lookups=ReadLookups(reader);
			foreach(Models.Home.Language language in lookups.languages) model.langs.Add(language);

			//read matches:
			reader.NextResult();
			while(reader.Read()) {
				int id=(int)reader["id"];
				string json=(string)reader["json"];
				model.matches.Add(Prettify.Entry(id, json, lookups));
			}

			//read pager:
			reader.NextResult();
			if(reader.Read()) {
				int currentPage=(int)reader["currentPage"];
				int maxPage=(int)reader["maxPage"];
				model.pager=new Models.Home.Pager(currentPage, maxPage);
			}

			reader.Close();
			conn.Close();
		}

		/// <summary>Populates the view model of the page that lists all top-level domains.</summary>
		public static void DoDomains(Models.Home.Domains model) {
			SqlConnection conn=new SqlConnection(GetConnectionString());
			conn.Open();
			SqlCommand command=new SqlCommand("dbo.pub_domains", conn);
			command.CommandType=CommandType.StoredProcedure;
			SqlParameter param;
			param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			SqlDataReader reader=command.ExecuteReader();

			//read lookups:
			Models.Home.Lookups lookups=ReadLookups(reader);
			foreach(Models.Home.Metadatum md in lookups.domains) model.domains.Add(new Models.Home.DomainListing(md.id, md.name["ga"], md.name["en"]));

			reader.Close();
			conn.Close();
		}

		/// <summary>Populates the view model of the home page.</summary>
		public static void DoIndex(Models.Home.Index model) {
			SqlConnection conn=new SqlConnection(GetConnectionString());
			conn.Open();
			SqlCommand command=new SqlCommand("dbo.pub_index", conn);
			command.CommandType=CommandType.StoredProcedure;
			SqlDataReader reader=command.ExecuteReader();

			//read lookups:
			Models.Home.Lookups lookups=ReadLookups(reader);
			foreach(Models.Home.Metadatum md in lookups.domains) model.domains.Add(new Models.Home.DomainListing(md.id, md.name["ga"], md.name["en"]));

			reader.Close();
			conn.Close();
		}

		/// <summary>Populates the view model of the page that lists entries by domain.</summary>
		public static void DoDomain(Models.Home.Domain model) {
			SqlConnection conn=new SqlConnection(GetConnectionString());
			conn.Open();
			SqlCommand command=new SqlCommand("dbo.pub_domain", conn);
			command.CommandType=CommandType.StoredProcedure;
			SqlParameter param;
			param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@domID"; param.SqlDbType=SqlDbType.Int; param.Value=model.domID; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@subdomID"; param.SqlDbType=SqlDbType.Int; param.Value=model.subdomID; command.Parameters.Add(param);
			param=new SqlParameter(); param.ParameterName="@page"; param.SqlDbType=SqlDbType.Int; param.Value=model.page; command.Parameters.Add(param);
			SqlDataReader reader=command.ExecuteReader();

			//read lookups:
			Models.Home.Lookups lookups=ReadLookups(reader);
			if(lookups.domainsById.ContainsKey(model.domID)){
				Models.Home.Metadatum md=lookups.domainsById[model.domID];
				model.domain=new Models.Home.DomainListing(md.id, md.name["ga"], md.name["en"]);

				//flatten the list of subdomains:
				JArray jSubdoms=(JArray)md.jo.Property("subdomains").Value;
				model.subdomains=FlattenSubdomains(1, jSubdoms, null, model.subdomID);
			}

			//read matches:
			reader.NextResult();
			while(reader.Read()) {
				int id=(int)reader["id"];
				string json=(string)reader["json"];
				model.matches.Add(Prettify.Entry(id, json, lookups));
			}

			//read pager:
			reader.NextResult();
			if(reader.Read()) {
				int currentPage=(int)reader["currentPage"];
				int maxPage=(int)reader["maxPage"];
				model.pager=new Models.Home.Pager(currentPage, maxPage);
			}

			reader.Close();
			conn.Close();
		}

		public static List<Models.Home.SubdomainListing> FlattenSubdomains(int level, JArray jSubdoms, Models.Home.SubdomainListing parent, int currentID) {
			List<Models.Home.SubdomainListing> ret=new List<Models.Home.SubdomainListing>();
			for(int i=0; i<jSubdoms.Count; i++) {
				JObject jSubdom=(JObject)jSubdoms[i];
				int id=(int)jSubdom.Property("lid").Value;
				string titleGA=(string)((JObject)jSubdom.Property("title").Value).Property("ga").Value;
				string titleEN=(string)((JObject)jSubdom.Property("title").Value).Property("en").Value;
				Models.Home.SubdomainListing sd=new Models.Home.SubdomainListing(id, titleGA, titleEN, level, false);
				sd.parent=parent;
				ret.Add(sd);

				if(sd.id == currentID) {
					Models.Home.SubdomainListing obj=sd;
					do {obj.visible=true; obj=obj.parent;} while(obj!=null);
				}

				JArray mySubdoms=(JArray)jSubdom.Property("subdomains").Value;
				ret.AddRange(FlattenSubdomains(level+1, mySubdoms, sd, currentID));
			}
			return ret;
		}
	}
}
