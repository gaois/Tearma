﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace TearmaWeb.Models.Data
{
	public class Entry {
		public string dStatus;
		public List<int?> domains;
		public List<Desig> desigs;
		public Dictionary<string, string> intros;
		public List<Definition> definitions;
		public List<Example> examples;
		public List<int> xrefs;
	}

	public class Example {
		public Dictionary<string, List<string>> texts;
		public int nonessential;
	}

	public class Definition {
		public Dictionary<string, string> texts;
		public List<int?> domains;
		public int nonessential;
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
		[JsonConverter(typeof(Controllers.IntegerJsonConverter))] public int start;
		[JsonConverter(typeof(Controllers.IntegerJsonConverter))] public int stop;
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