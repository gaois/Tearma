using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TearmaWeb.Models.Home;

namespace TearmaWeb.Controllers
{
    public class Broker {
        private readonly string _connectionString;

        public Broker(IConfiguration configuration) {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private static Lookups ReadLookups(SqlDataReader reader) {
            Lookups lookups=new Lookups();

			//read lingo config:
			if(reader.Read()) {
				string json=(string)reader["json"];
				JObject jo=JObject.Parse(json);
				JArray ja=(JArray)jo.Property("languages")?.Value ?? new JArray();
				for(int i=0; i<ja.Count; i++) {
					JObject jlang=(JObject)ja[i];
                    Language language=new Language(jlang);
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
                Metadatum metadatum;
				if(type=="posLabel") {
					metadatum=new Metadatum(id, jo, lookups.languages);
				} else {
					metadatum=new Metadatum(id, jo);
				}
				lookups.addMetadatatum(type, metadatum);
			}

			return lookups;
		}

		private static Dictionary<int, string> ReadXrefTargets(SqlDataReader reader) {
			Dictionary<int, string> xrefTargets=new Dictionary<int, string>();
			while(reader.Read()) {
				int id=(int)reader["id"];
				if(!xrefTargets.ContainsKey(id)) {
					string json=(string)reader["json"];
					xrefTargets.Add(id, json);
				}
			}
			return xrefTargets;
		}

		/// <summary>Takes the view model of the quick search page (with 'word' and 'lang' filled in) and populates all other properties from the database.</summary>
		public void DoQuickSearch(QuickSearch model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_quicksearch", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
			        SqlParameter param;
			        param=new SqlParameter(); param.ParameterName="@word"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.word; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@super"; param.SqlDbType=SqlDbType.Bit; param.Value=model.super; command.Parameters.Add(param);
                    
                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups = ReadLookups(reader);

			            //read similars:
			            reader.NextResult();
			            while(reader.Read()) {
				            model.similars.Add((string)reader["similar"]);
			            }

			            //read languages in which matches have been found:
			            reader.NextResult();
			            while(reader.Read()) {
				            string abbr=(string)reader["lang"];
				            if(lookups.languagesByAbbr.ContainsKey(abbr)) model.langs.Add(lookups.languagesByAbbr[abbr]);
			            }

			            //determine the sorting language:
			            model.sortlang=model.lang;
			            if(model.sortlang=="" && model.langs.Count>0) {
				            model.sortlang=model.langs[0].abbr;
				            if(model.sortlang!="ga" && model.sortlang!="en") model.sortlang="ga";
			            }

			            //read xref targets:
			            reader.NextResult();
			            Dictionary<int, string> xrefTargets=ReadXrefTargets(reader);

			            //read exact matches:
			            reader.NextResult();
			            while(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.exacts.Add(Prettify.Entry(id, json, lookups, model.sortlang, xrefTargets));
			            }

			            //read related matches:
			            reader.NextResult();
			            int relatedCount=0;
			            while(reader.Read()) {
				            relatedCount++;
				            if(relatedCount <= 100) {
					            int id=(int)reader["id"];
					            string json=(string)reader["json"];
					            model.relateds.Add(Prettify.Entry(id, json, lookups, model.sortlang));
				            }
			            }
			            if(relatedCount>100) model.relatedMore=true;

						//read auxilliary matches:
						if(model.super) {
							reader.NextResult();
							while(reader.Read()) {
								model.auxes.Add((string)reader["Placeholder"]);
							}
						}
                    }
                }
            }
		}

		/// <summary>Takes the view model of the advanced search page and populates the 'langs' property from the database.</summary>
		public void PrepareAdvSearch(AdvSearch model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_advsearch_prepare", conn)) {
			        command.CommandType=CommandType.StoredProcedure;

                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups=ReadLookups(reader);
			            foreach(Language language in lookups.languages) model.langs.Add(language);
			            foreach(Metadatum datum in lookups.posLabels) model.posLabels.Add(datum);
			            foreach(Metadatum datum in lookups.domains) {
				            //datum.subdomainsJson=FlattenSubdomainsIntoJson(datum);
				            model.domains.Add(datum);
			            }
                    }
			    }
			}
		}

		/// <summary>Takes the view model of the advanced search page (with 'word', 'length', 'extent', 'lang' and 'page' filled in) and populates all other properties from the database.</summary>
		public void DoAdvSearch(AdvSearch model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_advsearch", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
			        SqlParameter param;
			        param=new SqlParameter(); param.ParameterName="@word"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.word; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@length"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.length; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@extent"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.extent; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@pos"; param.SqlDbType=SqlDbType.Int; param.Value=model.posLabel; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@dom"; param.SqlDbType=SqlDbType.Int; param.Value=model.domainID; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@sub"; param.SqlDbType=SqlDbType.Int; param.Value=model.subdomainID; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@page"; param.SqlDbType=SqlDbType.Int; param.Value=model.page; command.Parameters.Add(param);

                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups =ReadLookups(reader);
			            foreach(Language language in lookups.languages) model.langs.Add(language);
			            foreach(Metadatum datum in lookups.posLabels) model.posLabels.Add(datum);
			            foreach(Metadatum datum in lookups.domains) {
				            //datum.subdomainsJson=FlattenSubdomainsIntoJson(datum);
				            model.domains.Add(datum);
			            }

			            //determine the sorting language:
			            model.sortlang=model.lang;
			            if(model.sortlang!="ga" && model.sortlang!="en") model.sortlang="ga";

			            //read xref targets:
			            reader.NextResult();
			            Dictionary<int, string> xrefTargets=ReadXrefTargets(reader);

			            //read matches:
			            reader.NextResult();
			            while(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.matches.Add(Prettify.Entry(id, json, lookups, model.sortlang, xrefTargets));
			            }

			            //read pager:
			            reader.NextResult();
			            if(reader.Read()) {
				            int currentPage=(int)reader["currentPage"];
				            int maxPage=(int)reader["maxPage"];
				            model.pager=new Pager(currentPage, maxPage);
			            }
                    }
			    }
			}
		}

		/// <summary>Populates the view model of the page that lists all top-level domains.</summary>
		public void DoDomains(Domains model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_domains", conn)) {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter param;
                    param = new SqlParameter(); param.ParameterName = "@lang"; param.SqlDbType = SqlDbType.NVarChar; param.Value = model.lang;
                    command.Parameters.Add(param);

                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups = ReadLookups(reader);
                        foreach (Metadatum md in lookups.domains) model.domains.Add(new Models.Home.DomainListing(md.id, md.name["ga"], md.name["en"]));
                    }
                }
            }
		}

		/// <summary>Populates the view model of the home page.</summary>
		public void DoIndex(Index model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_index", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
                    
                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups =ReadLookups(reader);
			            foreach(Metadatum md in lookups.domains) model.domains.Add(new DomainListing(md.id, md.name["ga"], md.name["en"]));

			            //read entry of the day:
			            reader.NextResult();
			            if(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.tod=Prettify.Entry(id, json, lookups, "ga");
			            }

			            //read recently changed entries:
			            reader.NextResult();
			            while(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.recent.Add(Prettify.EntryLink(id, json, "ga"));
			            }

			            //read news item:
			            reader.NextResult();
			            if(reader.Read()) {
				            model.newsGA=(string)reader["TextGA"];
				            model.newsEN=(string)reader["TextEN"];
			            }
			        }
                }
            }
		}

		/// <summary>Populates the view model of the single-entry page.</summary>
		public void DoEntry(Entry model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_entry", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
			        SqlParameter param;
			        param=new SqlParameter(); param.ParameterName="@id"; param.SqlDbType=SqlDbType.Int; param.Value=model.id; command.Parameters.Add(param);
                    
                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups =ReadLookups(reader);

			            //read xref targets:
			            reader.NextResult();
			            Dictionary<int, string> xrefTargets=ReadXrefTargets(reader);

			            //read the entry:
			            reader.NextResult();
			            if(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.entry=Prettify.Entry(id, json, lookups, "ga", xrefTargets);
			            }
                    }
                }
            }
        }

		public string GetSubdoms(int domID) {
			string ret="[]";
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_subdoms", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
			        SqlParameter param;
			        param=new SqlParameter(); param.ParameterName="@domID"; param.SqlDbType=SqlDbType.Int; param.Value=domID; command.Parameters.Add(param);
                    
                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups =ReadLookups(reader);
			            if(lookups.domainsById.ContainsKey(domID)){
                            Metadatum md =lookups.domainsById[domID];
							ret=FlattenSubdomainsIntoJson(md);
			            }
					}
				}
			}
			return ret;
		}

		/// <summary>Populates the view model of the page that lists entries by domain.</summary>
		public void DoDomain(Domain model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_domain", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
			        SqlParameter param;
			        param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@domID"; param.SqlDbType=SqlDbType.Int; param.Value=model.domID; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@subdomID"; param.SqlDbType=SqlDbType.Int; param.Value=model.subdomID; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@page"; param.SqlDbType=SqlDbType.Int; param.Value=model.page; command.Parameters.Add(param);
                    
                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups =ReadLookups(reader);
			            if(lookups.domainsById.ContainsKey(model.domID)){
                            Metadatum md =lookups.domainsById[model.domID];
				            model.domain=new DomainListing(md.id, md.name["ga"], md.name["en"]);

				            //flatten the list of subdomains:
				            JArray jSubdoms=(JArray)md.jo.Property("subdomains").Value;
				            model.subdomains=FlattenSubdomains(1, jSubdoms, null, model.subdomID);
			            }

			            //read xref targets:
			            reader.NextResult();
			            Dictionary<int, string> xrefTargets=ReadXrefTargets(reader);

			            //read matches:
			            reader.NextResult();
			            while(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.matches.Add(Prettify.Entry(id, json, lookups, model.lang, xrefTargets));
			            }

			            //read pager:
			            reader.NextResult();
			            if(reader.Read()) {
				            int currentPage=(int)reader["currentPage"];
				            int maxPage=(int)reader["maxPage"];
				            model.pager=new Pager(currentPage, maxPage);
			            }
			        }
                }
            }
		}

		public static List<SubdomainListing> FlattenSubdomains(int level, JArray jSubdoms, SubdomainListing parent, int currentID) {
			List<SubdomainListing> ret=new List<SubdomainListing>();
			for(int i=0; i<jSubdoms.Count; i++) {
				JObject jSubdom=(JObject)jSubdoms[i];
				int id=(int)jSubdom.Property("lid").Value;
				string titleGA=(string)((JObject)jSubdom.Property("title").Value).Property("ga").Value;
				string titleEN=(string)((JObject)jSubdom.Property("title").Value).Property("en").Value;
                SubdomainListing sd=new SubdomainListing(id, titleGA, titleEN, level, false);
				sd.parent=parent;
				ret.Add(sd);

				if(sd.id == currentID) {
                    SubdomainListing obj=sd;
					do {obj.visible=true; obj=obj.parent;} while(obj!=null);
				}

				JArray mySubdoms=(JArray)jSubdom.Property("subdomains").Value;
				ret.AddRange(FlattenSubdomains(level+1, mySubdoms, sd, currentID));
			}
			return ret;
		}

		public static string FlattenSubdomainsIntoJson(Metadatum domain) {
			JArray jSubdoms=(JArray)domain.jo.Property("subdomains").Value;
			List<SubdomainListing> subdomains=FlattenSubdomains(1, jSubdoms, null, 0);
			string ret="";
			foreach(SubdomainListing subdom in subdomains) {
				string title=subdom.name["ga"].Replace("\"", "\\\"")+" &middot; "+subdom.name["en"].Replace("\"", "\\\"");
                SubdomainListing parent =subdom.parent;
				while(parent != null) {
					if(parent.name["ga"] != parent.name["en"]) {
						title=parent.name["ga"].Replace("\"", "\\\"")+" &middot; "+parent.name["en"].Replace("\"", "\\\"")+" » "+title;
					} else {
						title=parent.name["ga"].Replace("\"", "\\\"")+" » "+title;
					}
					parent=parent.parent;
				}
				var s="{";
				s+="\"id\": "+subdom.id+",";
				s+="\"name\": \""+title+"\"";
				s+="}";
				if(ret!="") ret+=",";
				ret+=s;
			}
			ret="["+ret+"]";
			return ret;
		}

		/// <summary>Populates the view model of the term-of-the-day widget.</summary>
		public void DoTod(Models.Widgets.Tod model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_tod", conn)) {
			        command.CommandType=CommandType.StoredProcedure;

                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups=ReadLookups(reader);

			            //read entry of the day:
			            reader.NextResult();
			            if(reader.Read()) {
				            int id=(int)reader["id"];
				            string json=(string)reader["json"];
				            model.todID=id;
				            model.tod=Prettify.Entry(id, json, lookups, "ga");
			            }
                    }
                }
            }
		}
	}
}