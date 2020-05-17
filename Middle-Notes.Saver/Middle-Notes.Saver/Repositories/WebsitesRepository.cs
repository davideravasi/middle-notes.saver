using Middle_Notes.Saver.Helpers;
using Middle_Notes.Saver.Models;
using System.Collections.Generic;

namespace Middle_Notes.Saver.Repositories
{
    public class WebsitesRepository : BaseRepository
    {
        public WebsitesRepository(string connectionString) : base(connectionString)
        {
        }

        public List<Website> GetWebsitesId()
        {
            var websites = new List<Website>();
            var sql = "SELECT * FROM websites WHERE flag_deleted=0";
            using (var rdr = GetDataReader(sql))
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    while (rdr.Read())
                    {
                        websites.Add(new Website()
                        {
                            Id = GetInt(rdr, "website_id"),
                            CountryId = GetInt(rdr, "country_id"),
                            Host = GetString(rdr, "host"),
                            AggregateRegex = GetString(rdr, "regexp_saver_aggregate"),
                            DiscardJs = GetBoolean(rdr, "flag_discard_js"),
                            DiscardStyle = GetBoolean(rdr, "flag_discard_style"),
                            NeedBrowser = GetBoolean(rdr, "flag_enable_selenium"),
                            DelayAfterPageLoading = GetBoolean(rdr, "flag_delay_after_page_loading"),
                            ErrorIdentifiers = DeserializeHelpers.GetErrorIdentifiers(GetString(rdr, "selenium_error_identifiers"))
                        });
                    }
                }
            }
            return websites;
        }
    }
}
