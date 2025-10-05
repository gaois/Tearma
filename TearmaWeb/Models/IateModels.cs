using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace TearmaWeb.Models.Iate
{
	public class Tools {
		public static string SlashEncode(string text) {
			text=text.Replace(@"%", "%25");
			text=text.Replace(@"\", "$backslash;");
			text=text.Replace(@"/", "$forwardslash;");
			return text;
		}
	}

	/// <summary>Represents the contents of the IATE search page.</summary>
	public class Search {
		/// <summary>The string the user has typed into the search box.</summary>
		public string word="";

        /// <summary>The language code of the language in which the user has requested to see results. Empty string if all languages.</summary>
        public string lang = "";

        /// <summary>Whether this search is in superser mode (with the auxilliary glossary etc).</summary>
        public bool super = false;
        
		public string quickSearchUrl() {
			string ret = "/q/"+Uri.EscapeDataString(Tools.SlashEncode(this.word))+"/";
			return ret;
		}

		public int count = 0;
		public bool hasMore = false;
		public List<string> exacts = new List<string>();
		public List<string> relateds = new List<string>();
	}

}