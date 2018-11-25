using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TearmaWeb.Controllers {
	public class Prettify {
		private static Models.Home.Lookups Lookups;

		public static string Entry(int id, string json, Models.Home.Lookups lookups) {
			Models.Data.Entry entry=JsonConvert.DeserializeObject<Models.Data.Entry>(json);
			Prettify.Lookups=lookups;

			string ret="<div class='prettyEntry'>";
			foreach(Models.Data.DomainAssig obj in entry.domains) ret+=Prettify.DomainAssig(obj);

			{
				string html=""; bool withLangLabel=true;
				foreach(Models.Data.Desig desig in entry.desigs) {if(desig.term.lang=="ga") {html+=Prettify.Desig(desig, withLangLabel); withLangLabel=false;}}
				ret+="<div class='desigBlock left'>"+html+"</div>";
			}
			{
				string html=""; bool withLangLabel=true;
				foreach(Models.Data.Desig desig in entry.desigs) {if(desig.term.lang=="en") {html+=Prettify.Desig(desig, withLangLabel); withLangLabel=false;}}
				ret+="<div class='desigBlock right'>"+html+"</div>";
			}
			foreach(Models.Home.Language language in Prettify.Lookups.languages) {
				if(language.abbr!="ga" && language.abbr!="en") {
					string html=""; bool withLangLabel=true;
					foreach(Models.Data.Desig desig in entry.desigs) {if(desig.term.lang==language.abbr) {html+=Prettify.Desig(desig, withLangLabel); withLangLabel=false;}}
					if(html!="") ret+="<div class='desigBlock bottom'>"+html+"</div>";
				}
			}

			ret+="<div class='clear'></div>";
			ret+="</div>"; //.prettyEntry
			return ret;
		}

		public static string Desig(Models.Data.Desig desig, bool withLangLabel) {
			string ret="<div class='prettyDesig'>";
			if(desig.accept!=null && desig.accept > 0) {
				Models.Home.Metadatum md=Lookups.acceptLabelsById[(int)desig.accept];
				if(md.level<0) ret="<div class='prettyDesig grey'>";
			}
			if(withLangLabel) ret+=Prettify.Lang(desig.term.lang);
			ret+=Prettify.Wording(desig.term.wording, desig.term.annots);
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
			//ret+=JsonConvert.SerializeObject(desig);
			ret+="</div>";
			return ret;
		}

		public static string Wording(string wording, List<Models.Data.Annot> annots) {
			List<Char> chars=new List<Char>(); for(var i=0; i<wording.Length; i++) chars.Add(new Char{character=wording[i].ToString()});
			int index=0;
			foreach(Models.Data.Annot annot in annots) {
				int start=annot.start-1; if(start<0) start=0;
				int stop=annot.stop; if(stop>chars.Count) stop=chars.Count; if(stop==0) stop=chars.Count;
				for(int i=start; i<stop; i++) {
					if(annot.label.type == "posLabel") {
				        chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
						chars[i].markupAfter=chars[i].markupAfter+"</span>";
						Models.Home.Metadatum label=Lookups.posLabelsById[int.Parse(annot.label.value)];
						string symbol=label.abbr;
						if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+"<span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+label.name["ga"]+"/"+label.name["en"]+"'>"+symbol+"</span>";
					}
					else if(annot.label.type == "inflectLabel") {
				        chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
						chars[i].markupAfter=chars[i].markupAfter+"</span>";
						Models.Home.Metadatum label=Lookups.inflectLabelsById[int.Parse(annot.label.value)];
						string symbol=label.abbr;
						if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+"<span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+label.name["ga"]+"/"+label.name["en"]+"'>"+symbol+"</span>";
					}
					else if(annot.label.type == "langLabel") {
				        chars[i].markupBefore="<span class='char h"+index+"'>"+chars[i].markupBefore;
						chars[i].markupAfter=chars[i].markupAfter+"</span>";
						Models.Home.Language label=Lookups.languagesByAbbr[annot.label.value];
						string symbol=label.abbr.ToUpper();
						if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+"<span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+label.name["ga"]+"/"+label.name["en"]+"'>"+symbol+"</span>";
					}
					else if(annot.label.type == "symbol") {
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
						if(i==stop-1) chars[i].labelsAfter=chars[i].labelsAfter+"<span class='label "+annot.label.type+" hintable' onmouseover='hon(this, "+index+")' onmouseout='hoff(this, "+index+")' title='"+title+"'>"+symbol+"</span>";
					}
					else if(annot.label.type == "formatting") {
						chars[i].markupBefore="<span style='font-style: italic'>"+chars[i].markupBefore;
						chars[i].markupAfter=chars[i].markupAfter+"</span>";
					}
				}
				index++;
			}
			string s=""; foreach(Char c in chars) s+=c.markupBefore+c.character+c.markupAfter+c.labelsAfter;
			return "<span class='prettyWording'>"+s+"</span>";
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
			string ret="";
			ret="<span class='clarif'>";
			ret+="("+s+")";
			ret+="</span>";
			return ret;
		}

		public static string Lang(string abbr) {
			Models.Home.Language language=Lookups.languagesByAbbr[abbr];
			string ret="<span class='prettyLang hintable' title='"+language.name["ga"]+"/"+language.name["en"]+"'>";
			ret+=abbr.ToUpper();
			ret+="</span>";
			return ret;
		}

		public static string DomainAssig(Models.Data.DomainAssig da) {
			int domID = da.superdomain;
			Models.Home.Metadatum domain = Prettify.Lookups.domainsById[domID];
			string ret = "";
			if(domain != null) {

				string substepsGA = "";
				string substepsEN = "";
				int subdomID = da.subdomain ?? 0;
				if(subdomID > 0) {
					List<Models.Home.SubdomainListing> subs = Broker.FlattenSubdomains(1, domain.jo.Value<JArray>("subdomains"), null, subdomID);
					foreach(Models.Home.SubdomainListing sub in subs) {
						if(sub.visible) {
							substepsGA += " » " + sub.name["ga"];
							substepsEN += " » " + sub.name["en"];
						}
					}
				}

				ret += "<div class='prettyDomain'>";
				ret += "<div class='left'>" + domain.name["ga"] + substepsGA + "</div>";
				ret += "<div class='right'>" + domain.name["en"] + substepsEN + "</div>";
				ret += "<div class='clear'></div>";
				ret += "</div>"; //.prettyDomain
			}
			return ret;
		}


	}
}
