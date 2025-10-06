using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace TearmaWeb.Controllers {
    
    public class PrettifyIate {

        private static string RemoveHtml(string s) {
            s = Regex.Replace(s, "<[^>]+>", "");
            return s;
        }

        public static string Entry(JObject entry) {
            string s = "<div class='prettyEntry'>";

            if(entry["domains"] != null) {
                foreach(JObject domain in entry["domains"]) {
                    s += "<div class='prettyDomain'>";
                    if(domain["domain"]["path"] != null) {
                        foreach(string step in domain["domain"]["path"]) {
                            s += step + " » ";
                        }
                    }
                    s += domain["domain"]["name"];
                    s += "</div>"; //.prettyDomain
                }
            }

            s += "<div class='desigBlock left'>";
            if(entry["language"]["ga"] != null && entry["language"]["ga"]["term_entries"] != null) {
                bool isFirst = true;
                foreach(JObject term_entry in entry["language"]["ga"]["term_entries"]) {
                    string wording = RemoveHtml((string)term_entry["term_value"]);
                    s += $"<div class='prettyDesig' data-lang='ga' data-wording='{HttpUtility.HtmlEncode(wording)}'>";
                    if(isFirst) {
                        s += "<span class='prettyLang hintable' title='Gaeilge/Irish'>GA</span>";
                    }
                    s += $"<a class='prettyWording' href='/iate/{Uri.EscapeDataString(wording.Replace("/", "$forwardslash;").Replace("\\", "$backslash;"))}'>";
                    s += HttpUtility.HtmlEncode(wording);
                    s += "</a>";
                    s += "<span class='clickme' onclick='termMenuClick(this)'>▼</span>";
                    s += "<span class='copyme' onclick='copyClick(this)' title='Cóipeáil · Copy'><i class='far fa-copy'></i><i class='fas fa-check'></i></span>";
                    s += "</div>"; //.prettyDesig
                    isFirst = false;
                }
            }
            s += "</div>"; //.desigBlock.left

            s += "<div class='desigBlock right'>";
            if(entry["language"]["en"] != null && entry["language"]["en"]["term_entries"] != null) {
                bool isFirst = true;
                foreach(JObject term_entry in entry["language"]["en"]["term_entries"]) {
                    string wording = RemoveHtml((string)term_entry["term_value"]);
                    s += $"<div class='prettyDesig' data-lang='en' data-wording='{HttpUtility.HtmlEncode(wording)}'>";
                    if(isFirst) {
                        s += "<span class='prettyLang hintable' title='Béarla/English'>EN</span>";
                    }
                    s += $"<a class='prettyWording' href='/iate/{Uri.EscapeDataString(wording.Replace("/", "$forwardslash;").Replace("\\", "$backslash;"))}'>";
                    s += HttpUtility.HtmlEncode(wording);
                    s += "</a>";
                    s += "<span class='clickme' onclick='termMenuClick(this)'>▼</span>";
                    s += "<span class='copyme' onclick='copyClick(this)' title='Cóipeáil · Copy'><i class='far fa-copy'></i><i class='fas fa-check'></i></span>";
                    s += "</div>"; //.prettyDesig
                    isFirst = false;
                }
            }
            s += "</div>"; //.desigBlock.right

            if( (entry["language"]["en"] != null && entry["language"]["en"]["definition"] != null)
                || (entry["language"]["ga"] != null && entry["language"]["ga"]["definition"] != null)
            ) {
                s += "<div class='prettyDefinition'>";
                if(entry["language"]["ga"] != null && entry["language"]["ga"]["definition"] != null) {
                    s += $"<div class='left'>{HttpUtility.HtmlEncode(RemoveHtml((string)entry["language"]["ga"]["definition"]["value"]))}</div>";
                }
                if(entry["language"]["en"] != null && entry["language"]["en"]["definition"] != null) {
                    s += $"<div class='right'>{HttpUtility.HtmlEncode(RemoveHtml((string)entry["language"]["en"]["definition"]["value"]))}</div>";
                }
                s += "</div>"; //.prettyDefinition
            }

            s += "<div class='clear'></div>";

            //the entry's JSON as we got it from IATE, for debugging:
            //s += $"<div style='font-family: monospace; font-size: 0.75em; color: #999; line-height: 1.25em; height: 1.25em; overflow: hidden'>{HttpUtility.HtmlEncode(entry.ToString())}</div>";

            s += "</div>"; //.prettyEntry
            return s;
        }
    }

}
