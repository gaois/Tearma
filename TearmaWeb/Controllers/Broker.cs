using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TearmaWeb.Models;
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
				bool hasChildren=false; if((int)reader["hasChildren"]>0) hasChildren=true;
				JObject jo=JObject.Parse(json);
                Metadatum metadatum;
				if(type=="posLabel") {
					metadatum=new Metadatum(id, jo, hasChildren, lookups.languages);
				} else {
					metadatum=new Metadatum(id, jo, hasChildren);
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
								var coll=(string)reader["coll"];
								if(!model.auxes.ContainsKey(coll)) model.auxes.Add(coll, new List<System.Tuple<string, string>>());
								model.auxes[coll].Add(new System.Tuple<string, string>((string)reader["en"], (string)reader["ga"]));
							}
						}
                    }
                }
            }
		}

		/// <summary>Does the same thing as quick search but only returns the number of results.</summary>
		public void Peek(PeekResult model) {
            using(var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using(var command = new SqlCommand("dbo.pub_peek", conn)) {
                    command.CommandType=CommandType.StoredProcedure;
                    SqlParameter param;
                    param=new SqlParameter(); param.ParameterName="@word"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.word; command.Parameters.Add(param);

                    using(var reader = command.ExecuteReader()) {
                        while(reader.Read()) {
							var countExacts = (int)reader["CountExacts"];
							var countRelateds = (int)reader["CountRelateds"];
							model.count = countExacts + Math.Min(100, countRelateds);
							model.hasMore = (countRelateds > 100);
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
				            //if(datum.parentID==0){
					            model.domains.Add(datum);
							//}
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
			        param=new SqlParameter(); param.ParameterName="@page"; param.SqlDbType=SqlDbType.Int; param.Value=model.page; command.Parameters.Add(param);
					param=new SqlParameter(); param.ParameterName="@total"; param.SqlDbType=SqlDbType.Int; param.Value=model.total; param.Direction=ParameterDirection.InputOutput; command.Parameters.Add(param);

                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups =ReadLookups(reader);
			            foreach(Language language in lookups.languages) model.langs.Add(language);
			            foreach(Metadatum datum in lookups.posLabels) model.posLabels.Add(datum);
			            foreach(Metadatum datum in lookups.domains) {
							//if(datum.parentID == 0) {
					            model.domains.Add(datum);
							//}
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
					model.total=(int)command.Parameters["@total"].Value;
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
                        foreach (Metadatum md in lookups.domains) if(md.parentID==0) model.domains.Add(new Models.Home.DomainListing(md.id, md.name["ga"], md.name["en"], md.hasChildren));
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
			            foreach(Metadatum md in lookups.domains){
							if(md.parentID==0) {
								model.domains.Add(new DomainListing(md.id, md.name["ga"], md.name["en"]));
							}
						}

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

		/// <summary>Populates the view model of the page that lists entries by domain.</summary>
		public void DoDomain(Domain model) {
            using (var conn = new SqlConnection(_connectionString)) {
                conn.Open();

                using (var command = new SqlCommand("dbo.pub_domain", conn)) {
			        command.CommandType=CommandType.StoredProcedure;
			        SqlParameter param;
			        param=new SqlParameter(); param.ParameterName="@lang"; param.SqlDbType=SqlDbType.NVarChar; param.Value=model.lang; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@domID"; param.SqlDbType=SqlDbType.Int; param.Value=model.domID; command.Parameters.Add(param);
			        param=new SqlParameter(); param.ParameterName="@page"; param.SqlDbType=SqlDbType.Int; param.Value=model.page; command.Parameters.Add(param);
					param=new SqlParameter(); param.ParameterName="@total"; param.SqlDbType=SqlDbType.Int; param.Value=0; param.Direction=ParameterDirection.InputOutput; command.Parameters.Add(param);
                    
                    using (var reader = command.ExecuteReader()) {
                        //read lookups:
                        Lookups lookups=ReadLookups(reader);
			            if(lookups.domainsById.ContainsKey(model.domID)){
                            Metadatum md=lookups.domainsById[model.domID];
				            model.domain=new DomainListing(md.id, md.name["ga"], md.name["en"], md.hasChildren);
							while(lookups.domainsById.ContainsKey(md.parentID) && md.parentID != 0) {
								md=lookups.domainsById[md.parentID];
								if(md!=null) model.parents.Add(new DomainListing(md.id, md.name["ga"], md.name["en"], md.hasChildren));
							}
							model.parents.Reverse();
							foreach(Metadatum smd in lookups.domains) {
								if(smd .parentID == model.domID) {
									model.subdomains.Add(new DomainListing(smd.id, smd.name["ga"], smd.name["en"], smd.hasChildren));
								}
							}
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
					model.total=(int)command.Parameters["@total"].Value;
                }
            }
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