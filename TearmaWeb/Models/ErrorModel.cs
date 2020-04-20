using Ansa.Extensions;

namespace TearmaWeb.Models
{
    public class ErrorModel {
        public int? HttpStatusCode { get; set; }

        public string RequestID { get; set; }

        public bool ShowRequestID => RequestID.HasValue();
    }
}