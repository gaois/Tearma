using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Web;
using TearmaWeb.Models;
using TearmaWeb.Models.Home;
using TearmaWeb.Models.Iate;

namespace TearmaWeb.Controllers {

    public class IateBroker {
        private readonly string _connectionString;
        private readonly string _iateUsername;
        private readonly string _iatePassword;

        public IateBroker(IConfiguration configuration) {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _iateUsername = configuration.GetValue<string>("IATE:Username");
            _iatePassword= configuration.GetValue<string>("IATE:Password");
        }

        private string GetAccessToken() {
            string ret = "";
            var client = new HttpClient();
            var request = new HttpRequestMessage {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://iate.europa.eu/uac-api/oauth2/token?grant_type=password&username="+HttpUtility.UrlEncode(_iateUsername)+"&password="+HttpUtility.UrlEncode(_iatePassword)),
                Headers =
                {
                    { "accept", "application/vnd.iate.token+json; version=2" },
                },
                Content = new StringContent("") {
                    Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
                        }
                }
            };
            using(var response = client.SendAsync(request).GetAwaiter().GetResult()) {
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                JObject json = JObject.Parse(body);
                ret = (string)json["tokens"][0]["access_token"];
            }
            return ret;
        }

        /// <summary>Find out if IATE has any matches for this query.</summary>
        public void Peek(PeekResult model) {
            string token = this.GetAccessToken();

            var client = new HttpClient();
            var request = new HttpRequestMessage {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://iate.europa.eu/em-api/entries/_search?limit=1&expand=false"),
                Headers =
                {
                    { "accept", "application/vnd.iate.entry+json; version=2" },
                    { "Authorization", "Bearer "+token },
                },
                Content = new StringContent("{\"sources\": [\"en\", \"ga\"], \"targets\": [\"ga\", \"en\"], \"query\": \""+model.word+"\", \"query_operator\": 1}")
                {
                    Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                }
            };
            using(var response = client.SendAsync(request).GetAwaiter().GetResult()) {
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                JObject json = JObject.Parse(body);
                int count = (int)json["size"];

                model.count = Math.Min(100, count);
                model.hasMore = (count > 100);
            }
        }

        /// <summary>Takes the view model of the IATE search page and populates it.</summary>
        public void DoSearch(Search model) {
            string token = this.GetAccessToken();

            var client = new HttpClient();
            var request = new HttpRequestMessage {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://iate.europa.eu/em-api/entries/_msearch?fields_set_name=minimal"),
                Headers =
                {
                    { "accept", "application/vnd.iate.entry+json; version=2" },
                    { "Authorization", "Bearer "+token },
                },
                Content = new StringContent("[\n{\"limit\": 5, \"expand\": true, \"search_request\": {\"sources\": [\"en\", \"ga\"], \"targets\": [\"ga\", \"en\"], \"query\": \""+model.word+"\", \"query_operator\": 3}},\n{\"limit\": 100, \"expand\": true, \"search_request\": {\"sources\": [\"en\", \"ga\"], \"targets\": [\"ga\", \"en\"], \"query\": \""+model.word+"\", \"query_operator\": 1}}\n]")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };
            using(var response = client.SendAsync(request).GetAwaiter().GetResult()) {
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                JObject json = JObject.Parse(body);
                int count = (int)json["responses"][1]["size"];

                model.count = Math.Min(100, count);
                model.hasMore = (count > 100);

                if(json["responses"][0]["items"] != null) {
                    foreach(JObject entry in json["responses"][0]["items"]) {
                        string s = "";
                        if(entry["language"]["ga"] != null && entry["language"]["ga"]["term_entries"] != null) {
                            foreach(JObject term_entry in entry["language"]["ga"]["term_entries"]) {
                                s += "<div>ga: "+term_entry["term_value"]+"</div>";
                            }
                        }
                        if(entry["language"]["ga"] != null && entry["language"]["en"]["term_entries"] != null) {
                            foreach(JObject term_entry in entry["language"]["en"]["term_entries"]) {
                                s += "<div>en: "+term_entry["term_value"]+"</div>";
                            }
                        }
                        model.exacts.Add(s);
                        //model.exacts.Add(entry.ToString());
                    }
                }

                if(json["responses"][1]["items"] != null) {
                    foreach(JObject entry in json["responses"][1]["items"]) {
                        string s = "";
                        if(entry["language"]["ga"] != null && entry["language"]["ga"]["term_entries"] != null) {
                            foreach(JObject term_entry in entry["language"]["ga"]["term_entries"]) {
                                s += "<div>ga: "+term_entry["term_value"]+"</div>";
                            }
                        }
                        if(entry["language"]["en"] != null && entry["language"]["en"]["term_entries"] != null) {
                            foreach(JObject term_entry in entry["language"]["en"]["term_entries"]) {
                                s += "<div>en: "+term_entry["term_value"]+"</div>";
                            }
                        }
                        model.relateds.Add(s);
                        //model.relateds.Add(entry.ToString());
                    }
                }

            }
        }
    }

}
