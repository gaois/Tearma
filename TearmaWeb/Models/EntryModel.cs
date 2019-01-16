using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TearmaWeb.Models.Data {
	
	public class Entry {
		public List<DomainAssig> domains;
		public List<Desig> desigs;
	}

	public class DomainAssig {
		public int superdomain;
		public int? subdomain;
	}

	public class Desig {
		public Term term;
		public int? accept;
		public string clarif;
		public int nonessential;
	}

	public class Term {
		public string lang;
		public string wording;
		public List<Annot> annots;
		public List<Inflect> inflects;
	}

	public class Annot {
		public int start;
		public int stop;
		public AnnotLabel label;
	}

	public class AnnotLabel {
		public string type;
		public string value;
	}

	public class Inflect {
		public int label;
		public string text;
	}

}
