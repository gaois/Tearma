using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace TearmaWeb.Models {

    /// <summary>Represents the result of peeking, either into IATE or into Téarma.</summary>
    public class PeekResult {
        //input:
        public string word = "";
        //output:
        public int count = 0;
        public bool hasMore = false;
    }

}
