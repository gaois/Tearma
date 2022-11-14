using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.IO;

namespace TearmaWeb.Controllers
{
    public class Prettify {
		public static string ContentPath=""; //set by Startup.cs; this tells me where to look for sound files
		private static Models.Home.Lookups Lookups;

		//Get the paths of all soundfiles (if any) for this term:
		private static Dictionary<string, string> GetSounds(string lang, string wording) {
			Dictionary<string, string> ret = new Dictionary<string, string>();
			if (lang == "ga") {
				string dirPath = Path.Combine(ContentPath, "wwwroot\\sounds");
				wording = string.Concat(wording.Split(Path.GetInvalidFileNameChars())); //remove invalid filename characters
                string pattern = "*__" + wording.Replace(" ", "_") + ".wav";
				foreach (string filePath in System.IO.Directory.GetFiles(dirPath, pattern, SearchOption.AllDirectories))
				{
					string webFilePath="/sounds"+filePath.Substring(dirPath.Length).Replace("\\", "/");
					string dialectAbbr = Path.GetFileName(filePath).Substring(0, 1);
					if(!ret.ContainsKey(dialectAbbr)) ret.Add(dialectAbbr, webFilePath);
				}
			}
			return ret;
        }
        
		public static string EntryLink(int id, string json, string primLang) {
			Models.Data.Entry entry=JsonConvert.DeserializeObject<Models.Data.Entry>(json);

			string leftLang=primLang;
			string rightLang="en"; if(primLang=="en") rightLang="ga";

			string ret="<a class='prettyEntryLink' href='/id/"+id+"/'>";
			ret+="<span class='bullet'>#</span>&nbsp;";

			string html="";
			foreach(Models.Data.Desig desig in entry.desigs) if(desig.term.lang==leftLang && desig.nonessential==0) {
				if(html!="") html+=" &middot; ";
				html+="<span class='term left'>"+desig.term.wording+"</span>";
			}
			foreach(Models.Data.Desig desig in entry.desigs) if(desig.term.lang==rightLang && desig.nonessential==0) {
				if(html!="") html+=" &middot; ";
				html+="<span class='term right'>"+desig.term.wording+"</span>";
			}
			ret+=html;
			if(html=="") ret+=id;

			ret+="</a>"; //.prettyEntryLink
			return ret;
		}

		public static string Entry(int id, string json, Models.Home.Lookups lookups, string primLang) {
			return Entry(id, json, lookups, primLang, new Dictionary<int, string>());
		}

		public static string Entry(int id, string json, Models.Home.Lookups lookups, string primLang, Dictionary<int, string> xrefTargets) {
			Models.Data.Entry entry=JsonConvert.DeserializeObject<Models.Data.Entry>(json);
			Prettify.Lookups=lookups;

			string leftLang=primLang;
			string rightLang="en"; if(primLang=="en") rightLang="ga";

			string ret="<div class='prettyEntry'>";

			//permalink:
			ret +="<a class='permalink' href='/id/"+id+"/'>#</a>";

			//domains:
			foreach(int? obj in entry.domains) ret+=Prettify.DomainAssig(obj, leftLang, rightLang);

			//draft status:
			if(entry.dStatus=="0") {
                var label = new Dictionary<string, string>
                {
                    { "ga", "DRÉACHT-IONTRÁIL" },
                    { "en", "DRAFT ENTRY" }
                };

                var labelLeft = label.ContainsKey(leftLang) ? label[leftLang] : label["ga"];
                var labelRight = label.ContainsKey(rightLang) ? label[rightLang] : label["en"];

                ret += "<div class='prettyStatus'>";
				ret += "<div class='left' lang='" + leftLang + "'>" + labelLeft + "</div>";
				ret += "<div class='right' lang='" + rightLang + "'>" + labelRight + "</div>";
				ret += "<div class='clear'></div>";
				ret += "</div>";
			}

			//desigs and intros:
			{
				string html=""; bool withLangLabel=true;
				foreach(Models.Data.Desig desig in entry.desigs) {if(desig.term.lang==leftLang) {html+=Prettify.Desig(desig, withLangLabel); withLangLabel=false;}}
				if(entry.intros.ContainsKey(leftLang) && entry.intros[leftLang]!="")
                    html +="<div class='intro'><span>("+entry.intros[leftLang]+")</span></div>";
				ret+="<div class='desigBlock left'>"+html+"</div>";
			}
			{
				string html=""; bool withLangLabel=true;
				foreach(Models.Data.Desig desig in entry.desigs) {if(desig.term.lang==rightLang) {html+=Prettify.Desig(desig, withLangLabel); withLangLabel=false;}}
				if(entry.intros.ContainsKey(rightLang) && entry.intros[rightLang]!="")
					html+="<div class='intro'><span>("+entry.intros[rightLang]+")</span></div>";
				ret+="<div class='desigBlock right'>"+html+"</div>";
			}
			foreach(Models.Home.Language language in Prettify.Lookups.languages) {
				if(language.abbr!="ga" && language.abbr!="en") {
					string html=""; bool withLangLabel=true;
					foreach(Models.Data.Desig desig in entry.desigs) {if(desig.term.lang==language.abbr) {html+=Prettify.Desig(desig, withLangLabel); withLangLabel=false;}}
					if(html!="") ret+="<div class='desigBlock bottom'>"+html+"</div>";
				}
			}

			//definitions:
			foreach(Models.Data.Definition obj in entry.definitions) ret+=Prettify.Definition(obj, leftLang, rightLang);

			//examples:
			foreach(Models.Data.Example obj in entry.examples) ret+=Prettify.Example(obj, leftLang, rightLang);

			//xrefs:
			if(entry.xrefs!=null && entry.xrefs.Count > 0) {
				string subret="";
				int count=0;
				subret+="<div class='prettyXrefs'>";
				subret+="<div class='title'><span class='title'><span class='ga' lang='ga'>FÉACH FREISIN</span> &middot; <span class='en' lang='en'>SEE ALSO</span></span></div>";
				foreach(int xrefID in entry.xrefs) {
					if(xrefTargets.ContainsKey(xrefID)) {
						string html=EntryLink(xrefID, xrefTargets[xrefID], primLang);
						subret+=" <span class='xref'>"+html+"</span>";
						count++;
					}
				}
				subret+="</div>";
				if(count>0) ret+=subret;
			}

            ret += "<div class='clear'></div>";
            
            ret += "<a class='detailsIcon showDetails' style='display: none' href='javascript:void(null)' onclick='showDetails(this)'><span class='icon fas fa-angle-down'></span> <span class='ga'>Taispeáin breis sonraí</span> &middot; <span class='en'>Show more details</span></a>";
            ret += "<a class='detailsIcon hideDetails' style='display: none' href='javascript:void(null)' onclick='hideDetails(this)'><span class='icon fas fa-angle-up'></span> <span class='ga'>Folaigh sonraí breise</span> &middot; <span class='en'>Hide details</span></a>";

			ret+="</div>"; //.prettyEntry
			return ret;
		}

		public static string Desig(Models.Data.Desig desig, bool withLangLabel) {
			string grey="";
			if(desig.accept!=null && desig.accept > 0 && Lookups.acceptLabelsById.ContainsKey((int)desig.accept)) {
				Models.Home.Metadatum md=Lookups.acceptLabelsById[(int)desig.accept];
				if(md.level<0) grey=" grey";
			}
			string nonessential=(desig.nonessential==1 ? " nonessential" : "");
			string ret="<div class='prettyDesig"+grey+nonessential+"' data-lang='"+desig.term.lang+"' data-wording='"+HtmlEncoder.Default.Encode(desig.term.wording)+"'>";
			if(withLangLabel) ret+=Prettify.Lang(desig.term.lang);
			
			Dictionary<string,string> sounds=GetSounds(desig.term.lang, desig.term.wording);
			if (sounds.Count > 0) {
				ret += "<span class='playme' onclick='playerMenuClick(this)'";
				foreach(string dialectKey in sounds.Keys)
				{
					ret += " data-"+dialectKey+"='"+sounds[dialectKey]+"'";
				}
				ret += "><i class=\"fas fa-volume-up\"></i></span> ";
			}

			ret+=Prettify.Wording(desig.term.lang, desig.term.wording, desig.term.annots);
			ret+="<span class='clickme' onclick='termMenuClick(this)'>▼</span>";
			if(desig.accept!=null && desig.accept>0) ret+=" "+Prettify.Accept(desig.accept ?? 0);
			if(desig.clarif!=null && desig.clarif!="") ret+=" "+Prettify.Clarif(desig.clarif);
			if(desig.term.inflects.Count>0){
				ret+="<div class='inflects'>";
				bool isFirst=true;
				foreach(Models.Data.Inflect inflect in desig.term.inflects) {
					if(!isFirst) ret+=", ";
					ret+=Prettify.Inflect(inflect);
					isFirst=false;
				}
				ret+="</div>"; //.inflects
			}
			ret+="</div>";
			return ret;
		}

		public static string Wording(string lang, string wording, List<Models.Data.Annot> annots) {
			List<Char> chars=new List<Char>(); for(var i=0; i<wording.Length; i++) chars.Add(new Char{character=wording[i].ToString()});
			int index=0;
			foreach(Models.Data.Annot annot in annots) {
				int start=annot.start-1; if(start<0) start=0;
				int stop=annot.stop; if(stop>chars.Count) stop=chars.Count; if(stop==0) stop=chars.Count;
				for(int i=start; i<stop; i++) {
					if(annot.label.type == "posLabel") {
						if(Lookups.posLabelsById.ContainsKey(int.Parse(annot.label.value))) {
							chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
							chars[i].markupAfter=chars[i].markupAfter+"</span>";
							Models.Home.Metadatum label=Lookups.posLabelsById[int.Parse(annot.label.value)];
							string symbol=label.abbr;
							if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+" <span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+label.name["ga"]+"/"+label.name["en"]+"'>"+symbol+"</span>";
						}
					}
					else if(annot.label.type == "inflectLabel") {
						if(Lookups.inflectLabelsById.ContainsKey(int.Parse(annot.label.value))) {
							chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
							chars[i].markupAfter=chars[i].markupAfter+"</span>";
							Models.Home.Metadatum label=Lookups.inflectLabelsById[int.Parse(annot.label.value)];
							string symbol=label.abbr;
							if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+" <span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+label.name["ga"]+"/"+label.name["en"]+"'>"+symbol+"</span>";
						}
					}
					else if(annot.label.type == "langLabel") {
						if(Lookups.languagesByAbbr.ContainsKey(annot.label.value)) {
							chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
							chars[i].markupAfter=chars[i].markupAfter+"</span>";
							Models.Home.Language label=Lookups.languagesByAbbr[annot.label.value];
							string symbol=label.abbr.ToUpper();
							if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+" <span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+label.name["ga"]+"/"+label.name["en"]+"'>"+symbol+"</span>";
						}
					}
					else if(annot.label.type == "symbol" && annot.label.value!="proper") {
				        chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
						chars[i].markupAfter=chars[i].markupAfter+"</span>";
						string symbol="";
						if(annot.label.value=="tm") symbol="<span style='position: relative; top: -5px; font-size: 0.5em'>TM</span>";
						if(annot.label.value=="regtm") symbol="®";
						if(annot.label.value=="proper") symbol="¶";
						string title="";
						if(annot.label.value=="tm") title="trádmharc/trademark";
						if(annot.label.value=="regtm") title="trádmharc cláraithe/registered trademark";
						if(annot.label.value=="proper") title="ainm dílis/proper noun";
						if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+" <span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+title+"'>"+symbol+"</span>";
					}
					else if(annot.label.type == "formatting") {
						chars[i].markupBefore="<span style='font-style: italic'>"+chars[i].markupBefore;
						chars[i].markupAfter=chars[i].markupAfter+"</span>";
					}
				}
				index++;
			}
			string s=""; foreach(Char c in chars) {
				s +=c.markupBefore+c.character+c.markupAfter+c.labelsAfter;
			}
			return "<a class='prettyWording' href='/q/"+Uri.EscapeDataString(wording.Replace("/", "$forwardslash;").Replace("\\", "$backslash;"))+"/"+lang+"/'>"+s+"</a>";
		}
		private class Char {
			public string character="";
			public string markupBefore="";
			public string markupAfter="";
			public string labelsAfter="";
		}

		public static string Inflect(Models.Data.Inflect inflect) {
			string ret="";
			if(Prettify.Lookups.inflectLabelsById.ContainsKey(inflect.label)) {
				Models.Home.Metadatum md=Prettify.Lookups.inflectLabelsById[inflect.label];
				ret+="<span class='inflect'>";
				ret+="<span class='abbr hintable' title='"+md.name["ga"]+"/"+md.name["en"]+"'>"+md.abbr+"</span>";
				ret+="&nbsp;";
				ret+="<span class='wording'>"+inflect.text+"</span>";
				ret+="</span>";
			}
			return ret;
		}

		public static string Accept(int id) {
			string ret="";
			if(Prettify.Lookups.acceptLabelsById.ContainsKey(id)){
				Models.Home.Metadatum md=Prettify.Lookups.acceptLabelsById[id];
				ret="<span class='accept'>";
				ret+=md.name["ga"];
				ret+="/";
				ret+=md.name["en"];
				ret+="</span>";
			}
			return ret;
		}

		public static string Clarif(string s) {
			string ret="<span class='clarif'>";
			ret+="("+s+")";
			ret+="</span>";
			return ret;
		}

		public static string Lang(string abbr) {
			string ret="";
			if(Lookups.languagesByAbbr.ContainsKey(abbr)){
				Models.Home.Language language=Lookups.languagesByAbbr[abbr];
				ret="<span class='prettyLang hintable' title='"+language.name["ga"]+"/"+language.name["en"]+"'>";
				ret+=abbr.ToUpper();
				ret+="</span>";
			}
			return ret;
		}

		public static string DomainAssig(int? domID, string leftLang, string rightLang) {
			string ret = "";
			if(domID != null && Lookups.domainsById.ContainsKey((int)domID)) {
				Models.Home.Metadatum domain = Lookups.domainsById[(int)domID];
				string stepsLeft = "";
				string stepsRight = "";
				int recursionCounter=0;
				while(domain != null) {
					if(stepsLeft!="") stepsLeft=""+" » "+stepsLeft;
					if(stepsRight!="") stepsRight=""+" » "+stepsRight;

					stepsLeft  = (domain.name.ContainsKey(leftLang) ? domain.name[leftLang] : domain.name["ga"])   + stepsLeft;
					stepsRight = (domain.name.ContainsKey(rightLang) ? domain.name[rightLang] : domain.name["en"]) + stepsRight;

					recursionCounter++;
					domain = Lookups.domainsById.ContainsKey(domain.parentID) && domain.parentID>0 && recursionCounter<10 
						? Lookups.domainsById[domain.parentID] : null;
				}
				ret += "<div class='prettyDomain'>";
				ret += "<div class='left'><a href='/dom/"+domID+"/"+leftLang+"/'>" + stepsLeft + "</a></div>";
				ret += "<div class='right'><a href='/dom/"+domID+"/"+rightLang+"/'>" + stepsRight + "</a></div>";
				ret += "<div class='clear'></div>";
				ret += "</div>"; //.prettyDomain
			}
			return ret;
		}

		public static string DomainAssig(int? domID, string lang) {
			string ret = "";
			if(domID != null & Prettify.Lookups.domainsById.ContainsKey((int)domID)) {
				Models.Home.Metadatum domain = Prettify.Lookups.domainsById[(int)domID];
				string steps = "";
				int recursionCounter=0;
				while(domain != null) {
					if(steps!="") steps=""+" » "+steps;
					steps = (domain.name.ContainsKey(lang) ? domain.name[lang] : domain.name["ga"]) + steps;
					recursionCounter++;
					domain = (domain.parentID>0 && recursionCounter<10) ? Prettify.Lookups.domainsById[domain.parentID] : null;
				}
				ret += "<span class='prettyDomainInline'>";
				ret += "<a href='/dom/"+domID+"/"+lang+"/'>" + steps + "</a>";
				ret += "</span>"; //.prettyDomainInline
			}
			return ret;
		}

		public static string Definition(Models.Data.Definition def, string leftLang, string rightLang) {
			string ret = "";
			string nonessential=(def.nonessential==1 ? " nonessential" : "");
			ret += "<div class='prettyDefinition"+nonessential+"'>";
			ret += "<div class='left'>";
				foreach(int? da in def.domains) if(da!=null) ret+=DomainAssig(da, leftLang)+" ";
                if (def.texts.ContainsKey(leftLang))
				    ret += def.texts[leftLang];
			ret += "</div>";
			ret += "<div class='right'>";
				foreach(int? da in def.domains) if(da!=null) ret+=DomainAssig(da, rightLang)+" ";
				ret += def.texts[rightLang];
			ret += "</div>";
			ret += "<div class='clear'></div>";
			ret += "</div>"; //.prettyDefinition
			return ret;
		}

		public static string Example(Models.Data.Example ex, string leftLang, string rightLang) {
			string ret = "";
			string nonessential=(ex.nonessential==1 ? " nonessential" : "");
			ret += "<div class='prettyExample"+nonessential+"'>";
			ret += "<div class='left'>";
                if (ex.texts.ContainsKey(leftLang))
				    foreach(string text in ex.texts[leftLang]) ret += "<div class='text'>"+text+"</div>";
			ret += "</div>";
			ret += "<div class='right'>";
                if (ex.texts.ContainsKey(rightLang))
					foreach(string text in ex.texts[rightLang]) ret += "<div class='text'>"+text+"</div>";
			ret += "</div>";
			ret += "<div class='clear'></div>";
			ret += "</div>"; //.prettyExample
			return ret;
		}

	}

	//custom JSON converter to convert partly-faulty integers like "9+" and "1o" into correct integers like 9 and 10
	//adapted from http://jigarbagadai.blogspot.com/2012/11/custom-json-convertor-for-nullable.html
	public class IntegerJsonConverter: JsonConverter{
        public override bool CanConvert(Type objectType){
            return objectType == typeof(int?) || objectType == typeof(int);
        }
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer){
            if(reader.TokenType == JsonToken.Null) return null;
            if(reader.TokenType == JsonToken.Integer) return reader.Value;
            if(reader.TokenType == JsonToken.String) {

				string text=(string)reader.Value;
				text=text.Replace("o", "0");
				text=text.Replace("O", "0");
				text=Regex.Replace(text, @"[^0-9]", "");

                if(string.IsNullOrEmpty(text)) return null;
				int num; if(int.TryParse(text, out num)) return num;
            }
			return 0;
        }
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue(value);
        }
    }
}
