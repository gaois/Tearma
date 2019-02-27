using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Newtonsoft.Json.Linq;

namespace TearmaWeb.Models.Widgets {

	/// <summary>Represents the contents of the searchbox widget.</summary>
	public class Box {
	}

	/// <summary>Represents the contents of the 'Term of the day' widget.</summary>
	public class Tod {

		/// <summary>Term of the day.</summary>
		public string tod="";
		public int todID=0;

	}

}
