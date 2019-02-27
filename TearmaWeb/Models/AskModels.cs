using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Newtonsoft.Json.Linq;

namespace TearmaWeb.Models.Ask {

	public class Ask {
		public string mode="empty"; //empty|error|thanks

		//Submitted data:
		public string termEN="";
		public string termXX="";
		public string context="";
		public string def="";
		public string example="";
		public string other="";
		public string termGA="";
		public string name="";
		public string email="";
		public string phone="";
	}


}
