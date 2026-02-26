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

        private static string RemoveSomeHtml(string s) {
            s = Regex.Replace(s, "<(?!b>|/b>|i>|/i>|strong>|/strong>|em>|/em>])[^>]+>", "", RegexOptions.IgnoreCase);
            return s;
        }
        private static string RemoveAllHtml(string s) {
            s = Regex.Replace(s, "<[^>]+>", "");
            return s;
        }

        public static string Entry(JObject entry, string leftlang, string rightlang) {
            string s = "<div class='prettyEntry'>";

            string id = (string)entry["id"];
            s += $"<a class='iateLink' target='_blank' href='https://iate.europa.eu/entry/result/{id}'>#{id} <i class=\"fas fa-external-link-alt\"></i></a>";

            if(entry["domains"] != null) {
                foreach(JObject domain in entry["domains"]) {
                    s += "<div class='prettyDomain iate'>";
                    if(domain["domain"]["path"] != null) {
                        foreach(string step in domain["domain"]["path"]) {
                            s += step + " » ";
                        }
                    }
                    s += domain["domain"]["name"];
                    s += "</div>"; //.prettyDomain
                }
            }

            string sLeft = "";
            string sRight = "";
            foreach(string lang in new List<string> { "ga", "en", "fr", "de" }) {
                string sBlock = "";
                if (entry["language"][lang] != null && entry["language"][lang]["term_entries"] != null) {
                    bool isFirst = true;
                    foreach (JObject term_entry in entry["language"][lang]["term_entries"]) {
                        string wording = RemoveAllHtml((string)term_entry["term_value"]);
                        sBlock += $"<div class='prettyDesig' data-lang='{lang}' data-wording='{HttpUtility.HtmlEncode(wording)}'>";
                        if (isFirst) {
                            if(lang=="ga") sBlock += "<span class='prettyLang hintable' title='Gaeilge/Irish'>GA</span>";
                            if(lang=="en") sBlock += "<span class='prettyLang hintable' title='Béarla/English'>EN</span>";
                            if(lang=="de") sBlock += "<span class='prettyLang hintable' title='Gearmáinis/German'>DE</span>";
                            if(lang=="fr") sBlock += "<span class='prettyLang hintable' title='Fraincis/French'>FR</span>";
                        }
                        if(lang=="ga" || lang == "en") {
                            sBlock += $"<a class='prettyWording' href='/q/{Uri.EscapeDataString(wording.Replace("/", "$forwardslash;").Replace("\\", "$backslash;"))}'>";
                            sBlock += HttpUtility.HtmlEncode(wording);
                            sBlock += "</a>";
                            sBlock += "<span class='clickme' onclick='termMenuClick(this)'>▼</span>";
                            sBlock += "<span class='copyme' onclick='copyClick(this)' title='Cóipeáil · Copy'><i class='far fa-copy'></i><i class='fas fa-check'></i></span>";
                        } else
                        {
                            sBlock += $"<span class='prettyWording'>";
                            sBlock += HttpUtility.HtmlEncode(wording);
                            sBlock += "</span>";
                        }
                        if (lang == "ga" && term_entry["contexts"] != null) {
                            foreach (JObject context in term_entry["contexts"]) {
                                sBlock += $"<div class='iateExample'>{RemoveSomeHtml((string)context["context"])}</div>";
                            }
                        }
                        sBlock += "</div>"; //.prettyDesig
                        isFirst = false;
                    }
                    if ((lang=="ga" || lang=="en") && entry["language"][lang]["definition"] != null)
                    {
                        sBlock += $"<div class='iateDefinition'>{RemoveSomeHtml((string)entry["language"][lang]["definition"]["value"])}</div>";
                    }
                }
                if (lang == leftlang) {
                    sLeft += sBlock;
                } else if(lang == rightlang) { 
                    sRight += sBlock;
                } else if(sBlock != "") {
                    if("en" == leftlang) {
                        sLeft += "<div class='desigBlock bottom'>" + sBlock + "</div>";
                    } else if("en" == rightlang) {
                        sRight += "<div class='desigBlock bottom'>" + sBlock + "</div>";
                    }
                }
            }
            s += "<div class='desigBlock left'>"+sLeft+"</div>";
            s += "<div class='desigBlock right'>"+sRight+"</div>";

            s += "<div class='clear'></div>";

            //the entry's JSON as we got it from IATE, for debugging:
            //s += $"<div style='font-family: monospace; font-size: 0.75em; color: #999; line-height: 1.25em; height: 1.25em; overflow: hidden'>{HttpUtility.HtmlEncode(entry.ToString())}</div>";

            s += "</div>"; //.prettyEntry
            return s;
        }
    }

}
