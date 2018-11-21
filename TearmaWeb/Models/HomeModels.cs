using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;

namespace TearmaWeb.Models.Home {

	/// <summary>Represents the names (in Irish and English) and abbreviation of a languaage.</summary>
	public class Language {
		/// <summary>The language code, eg. "ga".</example>
		public string abbr="";

		/// <summary>Human-readable names of the language in Irish ["ga"] and English ["en"].</summary>
		public Dictionary<string, string> name=new Dictionary<string, string>(); //eg. "en" => "Czech"

		public Language(string abbr, string nameGA, string nameEN) {
			this.abbr=abbr;
			this.name.Add("ga", nameGA);
			this.name.Add("en", nameEN);
		}
	}

	/// <summary>Represents the contents of the quick search page.</summary>
	public class QuickSearch {
		public QuickSearch() {
			this.similars.AddRange(new string[]{"suggestion1", "suggestion2", "suggestion3"});
			this.exacts.AddRange(new string[]{"<div>exact1</div>", "<div>exact2</div>", "<div>exact3</div>"});
			this.relateds.AddRange(new string[]{"<div>related1</div>", "<div>related2</div>", "<div>related3</div>"});
			this.relatedMore=true;
			this.langs.Add(new Language("ga", "Gaeilge", "Irish"));
			this.langs.Add(new Language("en", "Béarla", "English"));
			this.langs.Add(new Language("cs", "Seicis", "Czech"));
		}

		/// <summary>The string the user has typed into the search box.</summary>
		public string word="";

		/// <summary>The language code of the language in which the user has requested to see results. Empty string if all languages.</summary>
		public string lang="";

		/// <summary>Spelling suggestions.</summary>
		public List<string> similars=new List<string>();

		/// <summary>Exact matches in HTML.</summary>
		public List<string> exacts=new List<string>();

		/// <summary>Related matches in HTML.</summary>
		public List<string> relateds=new List<string>();

		/// <summary>Whether or not there are more related matches in the back-end database than those returned by this search.</summary>
		public bool relatedMore=false;

		/// <summary>The languages in which (exact and/or related) matches have been found.</summary>
		public List<Language> langs=new List<Language>();
	}

	/// <summary>Represents the contents of a pager.</summary>
	public class Pager {
		public bool needed=false;

		public int prevNum=0;
		public List<int> startNums=new List<int>();
		public bool preDots=false;
		public List<int> preNums=new List<int>();
		public int currentNum=0;
		public List<int> postNums=new List<int>();
		public bool postDots=false;
		public List<int> endNums=new List<int>();
		public int nextNum=0;

		public Pager(int currentPage, int maxPage) {
			if(maxPage > 1) {
				this.needed=true;
				this.currentNum=currentPage;
				if(currentPage-1>0) this.prevNum=currentPage-1;
				if(currentPage+1<=maxPage) this.nextNum=currentPage+1;

				if(currentPage <= 6) {
					for(int i=1; i<currentPage; i++) startNums.Add(i);
				} else {
					for(int i=1; i<=2; i++) startNums.Add(i);
					this.preDots=true;
					for(int i=currentPage-2; i<currentPage; i++) preNums.Add(i);
				}

				if(currentPage >= maxPage - 6) {
					for(int i=currentPage+1; i<=maxPage; i++) endNums.Add(i);
				} else {
					for(int i=currentPage+1; i<=currentPage+2; i++) postNums.Add(i);
					this.postDots=true;
					for(int i=maxPage-1; i<=maxPage; i++) endNums.Add(i);
				}
			}
		}
	}

	/// <summary>Represents the contents of the advanced search page.</summary>
	public class AdvSearch {
		public AdvSearch() {
			this.langs.Add(new Language("ga", "Gaeilge", "Irish"));
			this.langs.Add(new Language("en", "Béarla", "English"));
			this.langs.Add(new Language("cs", "Seicis", "Czech"));
			this.matches.AddRange(new string[]{"<div>match1</div>", "<div>match2</div>", "<div>match3</div>"});
		}

		/// <summary>The list of languages in the 'languages' listbox.</summary>
		public List<Language> langs=new List<Language>();

		/// <summary>The string the user has typed into the search box.</summary>
		public string word="";

		/// <summary>The length the user has selected: al|sw|mw</summary>
		public string length="";

		/// <summary>The extent the user has selected: al|st|ed|pt|md|ft</summary>
		public string extent="";

		/// <summary>The language code of the language in which the user has requested to see results. Empty string if all languages.</summary>
		public string lang="";

		/// <summary>The page the user has selected.</summary>
		public int page=0;

		/// <summary>Matches in HTML.</summary>
		public List<string> matches=new List<string>();

		/// <summary>The pager above and below the list of matches.</summary>
		public Pager pager;

		public string urlByPage(int page) {
			string ret="/plus/"+HtmlEncoder.Default.Encode(this.word)+"/"+this.length+"/"+this.extent+"/";
			if(this.lang!="") ret+=this.lang+"/";
			ret+=page+"/";
			return ret;
		}
	}

	/// <summary>Represents the names (in Irish and English) and numeric ID of a (top-level) domain.</summary>
	public class DomainListing {
		/// <summary>The numeric ID, eg. 43547.</example>
		public int id=0;

		/// <summary>Human-readable title of the domain in Irish ["ga"] and English ["en"].</summary>
		public Dictionary<string, string> name=new Dictionary<string, string>();

		public DomainListing(int id, string nameGA, string nameEN) {
			this.id=id;
			this.name.Add("ga", nameGA);
			this.name.Add("en", nameEN);
		}
	}

	/// <summary>
	/// Represents the contents of the page that lists all top-level domains.
	/// </summary>
	public class Domains {
		public Domains() {
			this.domains.Add(new DomainListing(123, "Aaa", "Aaa"));
			this.domains.Add(new DomainListing(456, "Bbb", "Bbb"));
			this.domains.Add(new DomainListing(789, "Ccc", "Ccc"));
		}

		/// <summary>The sorting language: "ga" or "en".</summary>
		public string lang="";

		/// <summary>The top-level domains.</summary>
		public List<DomainListing> domains=new List<DomainListing>();
	}

	/// <summary>Represents the names (in Irish and English) and numeric ID of a subdomain.</summary>
	public class SubdomainListing {
		/// <summary>The numeric ID, eg. 43547.</example>
		public int id=0;

		/// <summary>Human-readable title of the domain in Irish ["ga"] and English ["en"].</summary>
		public Dictionary<string, string> name=new Dictionary<string, string>();

		/// <summary>The indent level of the subdomain underneath the top-level domain. Child = 1, grandchild = 2 etc.</summary>
		public int level=0;

		/// <summary>Whether or not this subdomain should be visible even when hierarchy is collapsed.</summary>
		public bool visible=false;

		public SubdomainListing(int id, string nameGA, string nameEN, int level, bool visible) {
			this.id=id;
			this.name.Add("ga", nameGA);
			this.name.Add("en", nameEN);
			this.level=level;
			this.visible=visible;
		}
	}

	/// <summary>
	/// Represents the contents of the page that lists one top-level domain, its subdomains, and some entries.
	/// </summary>
	public class Domain {
		public Domain() {
			this.matches.AddRange(new string[]{"<div>match1</div>", "<div>match2</div>", "<div>match3</div>"});
			this.domain=new DomainListing(123, "Aaa", "Aaa");
			this.subdomains.Add(new SubdomainListing(12, "Foréimse 1", "Subdomain 1", 1, true));
			this.subdomains.Add(new SubdomainListing(34, "Foréimse 1.1", "Subdomain 1.1", 2, true));
			this.subdomains.Add(new SubdomainListing(56, "Foréimse 1.1.1", "Subdomain 1.1.1", 3, true));
			this.subdomains.Add(new SubdomainListing(21, "Foréimse 2", "Subdomain 2", 1, false));
			this.subdomains.Add(new SubdomainListing(43, "Foréimse 2.1", "Subdomain 2.1", 2, false));
			this.subdomains.Add(new SubdomainListing(65, "Foréimse 2.1.1", "Subdomain 2.1.1", 3, false));
}

		/// <summary>The sorting language: "ga" or "en".</summary>
		public string lang="";

		/// <summary>The domain ID the user has requested.</summary>
		public int domID=0;

		/// <summary>The top-level domain.</summary>
		public DomainListing domain=null;

		/// <summary>The subdomain ID the user has requested (0 if none).</summary>
		public int subdomID=0;

		/// <summary>A flattened list of all subdomains.</summary>
		public List<SubdomainListing> subdomains=new List<SubdomainListing>();

		/// <summary>The page the user has selected.</summary>
		public int page=0;

		/// <summary>Matches in HTML.</summary>
		public List<string> matches=new List<string>();

		/// <summary>The pager above and below the list of matches.</summary>
		public Pager pager;

		public string urlByPage(int page) {
			string ret="/dom/"+this.domID+"/";
			if(this.subdomID>0) ret+=this.subdomID+"/";
			ret+=this.lang+"/";
			ret+=page+"/";
			return ret;
		}

		public string urlByLang(string lang) {
			string ret="/dom/"+this.domID+"/";
			if(this.subdomID>0) ret+=this.subdomID+"/";
			ret+=lang+"/";
			return ret;
		}
	}

}
