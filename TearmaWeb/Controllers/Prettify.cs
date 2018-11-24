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
			return "<span class='prettyWording'>"+wording+"</span>";
		}

		public static string Inflect(Models.Data.Inflect inflect) {
			string ret="";
			if(Prettify.Lookups.inflectLabelsById.ContainsKey(inflect.label)) {
				Models.Home.Metadatum md=Prettify.Lookups.inflectLabelsById[inflect.label];
				ret+="<span class='inflect'>";
				ret+="<span class='abbr'>"+md.abbr+"</span>";
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
			string ret="<span class='prettyLang'>";
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
