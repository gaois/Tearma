using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Profiling.Internal;
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
using TearmaWeb.Models.Data;
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
                RequestUri = new Uri("https://iate.europa.eu/em-api/entries/_msearch?fields_set_name=minimal"),
                Headers =
                {
                    { "accept", "application/vnd.iate.entry+json; version=2" },
                    { "Authorization", "Bearer "+token },
                },
                Content = new StringContent($@"[
                    {{""limit"": 5, ""expand"": true, ""search_request"": {{""sources"": [""ga""], ""targets"": [""en""], ""query"": ""{model.word}"", ""query_operator"": 3}}}},
                    {{""limit"": 5, ""expand"": true, ""search_request"": {{""sources"": [""en""], ""targets"": [""ga""], ""query"": ""{model.word}"", ""query_operator"": 3}}}},
                    {{""limit"": 101, ""expand"": true, ""search_request"": {{""sources"": [""ga""], ""targets"": [""en""], ""query"": ""{model.word}"", ""query_operator"": 1}}}},
                    {{""limit"": 101, ""expand"": true, ""search_request"": {{""sources"": [""en""], ""targets"": [""ga""], ""query"": ""{model.word}"", ""query_operator"": 1}}}}
                ]") {
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

                List<string> entryURLs = new List<string>();
                for(int i = 0; i<4; i++) {
                    if(json["responses"][i]["items"] != null) {
                        foreach(JObject entry in json["responses"][i]["items"]) {
                            if(model.count<100) {
                                string entryURL = (string)entry["self"]["href"];
                                if(!entryURLs.Contains(entryURL)) {
                                    bool hasGA = false;
                                    bool hasEN = false;
                                    if(entry["language"]["ga"] != null && entry["language"]["ga"]["term_entries"] != null) {
                                        hasGA = true;
                                    }
                                    if(entry["language"]["en"] != null && entry["language"]["en"]["term_entries"] != null) {
                                        hasEN = true;
                                    }
                                    if(hasGA && hasEN) {
                                        entryURLs.Add(entryURL);
                                        model.count++;
                                    }
                                }
                            } else {
                                model.hasMore=true;
                            }
                        }
                    }
                }


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
                Content = new StringContent($@"[
                    {{""limit"": 5, ""expand"": true, ""search_request"": {{""sources"": [""ga""], ""targets"": [""en""], ""query"": ""{model.word}"", ""query_operator"": 3}}}},
                    {{""limit"": 5, ""expand"": true, ""search_request"": {{""sources"": [""en""], ""targets"": [""ga""], ""query"": ""{model.word}"", ""query_operator"": 3}}}},
                    {{""limit"": 101, ""expand"": true, ""search_request"": {{""sources"": [""ga""], ""targets"": [""en""], ""query"": ""{model.word}"", ""query_operator"": 1}}}},
                    {{""limit"": 101, ""expand"": true, ""search_request"": {{""sources"": [""en""], ""targets"": [""ga""], ""query"": ""{model.word}"", ""query_operator"": 1}}}}
                ]")
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

                List<int> entryIDs=new List<int>();
                for(int i=0; i<4; i++) {
                    if(json["responses"][i]["items"] != null) {
                        foreach(JObject entry in json["responses"][i]["items"]) {
                            if(model.count<100) {
                                int entryID = (int)entry["id"];
                                if(!entryIDs.Contains(entryID)){
                                    bool hasGA = (entry["language"]["ga"] != null && entry["language"]["ga"]["term_entries"] != null);
                                    bool hasEN = (entry["language"]["en"] != null && entry["language"]["en"]["term_entries"] != null);
                                    if(hasGA && hasEN) {
                                        string s = PrettifyIate.Entry(entry);
                                        if(i<2) {
                                            model.exacts.Add(s);
                                        } else {
                                            model.relateds.Add(s);
                                        }
                                        entryIDs.Add(entryID);
                                        model.count++;
                                    }
                                }
                            } else {
                                model.hasMore=true;
                            }
                            //model.exacts.Add(entry.ToString());
                        }
                    }
                }

            }
        }
    }

}
