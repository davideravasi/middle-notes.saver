using System.Collections.Generic;
using System.Net;

namespace Middle_Notes.Saver.Models
{
    public class Website
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public string Host { get; set; }
        public string AggregateRegex { get; set; }
        public bool DiscardJs { get; set; }
        public bool DiscardStyle { get; set; }
        public bool NeedBrowser { get; set; }
        public bool DelayAfterPageLoading { get; set; }
        public Dictionary<string, string> SessionStorageParameters { get; set; }
        public Dictionary<string, HttpStatusCode> ErrorIdentifiers { get; set; }
    }
}
