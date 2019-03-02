using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Newtonsoft.Json.Linq;

namespace TearmaWeb.Models.Info {

	/// <summary>Represents the contents of the download page.</summary>
	public class Download {
	}

	/// <summary>Represents the contents of the widgets page.</summary>
	public class Widgets {
	}

	/// <summary>Represents the contents of one info page.</summary>
	public class Topic {
		public string section=""; //eolas|help
		public string nickname=""; //<nickname>.<lang>.md
		public string lang=""; //ga|en
		public List<TocItem> toc=new List<TocItem>();
		public string body="";
	}

	public class TocItem {
		public string nickname;
		public Dictionary<string, string> title=new Dictionary<string, string>(); //ga > "..."", en > "..." 
		public TocItem(string nickname, string titleGA, string titleEN) {
			this.nickname=nickname;
			this.title.Add("ga", titleGA);
			this.title.Add("en", titleEN);
		}
	}

	public class Toc {
		public static List<TocItem> InfoToc() {
			List<TocItem> toc=new List<TocItem>();
			toc.Add(new TocItem("tionscadal", "Tionscadal téarma.ie", "The téarma.ie project"));
			toc.Add(new TocItem("stair", "Stair téarma.ie", "History of téarma.ie"));
			toc.Add(new TocItem("abhar", "Eolas faoin ábhar", "About the content"));
			toc.Add(new TocItem("coiste", "An Coiste Téarmaíochta", "Terminology Committee"));
			toc.Add(new TocItem("cosaint-sonrai", "Eolas cosanta sonraí", "Data protection information"));
			return toc;
		}
		public static List<TocItem> HelpToc() {
			List<TocItem> toc=new List<TocItem>();
			toc.Add(new TocItem("conas-usaid", "Conas an suíomh a úsáid", "How to use the website"));
			toc.Add(new TocItem("cad-is-tearma", "Cad is téarma ann?", "What is a term?"));
			toc.Add(new TocItem("cuardach-tapa", "Conas cuardach tapa a úsáid", "How to use quick search"));
			toc.Add(new TocItem("cuardach-casta", "Conas an cuardach casta a úsáid", "How to use advanced search"));
			toc.Add(new TocItem("saoroga", "Saoróga sa chuardach casta", "Wildcards in advanced search"));
			toc.Add(new TocItem("reimsi", "Conas brabhsáil de réir réimse", "How to browse by domain"));
			toc.Add(new TocItem("torthai-a-leamh", "Conas torthaí an chuardaigh a léamh", "How to read search results"));
			return toc;
		}
	}

}
