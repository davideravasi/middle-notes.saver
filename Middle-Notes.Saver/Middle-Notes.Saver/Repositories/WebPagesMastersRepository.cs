using Middle_Notes.Saver.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Middle_Notes.Saver.Repositories
{
    public class WebPagesMastersRepository : BaseRepository
    {
        public WebPagesMastersRepository(string connectionString) : base(connectionString)
        {
        }

        public IEnumerable<WebPage> GetWebPagesToSave(long websiteId)
        {
            var sql = "SELECT website_id, websites_pages_aggregate_id, url FROM websites_pages_masters " +
                "WHERE website_id=" + EscapeIntegerValue(websiteId) + " " +
                "AND is_done=0 AND is_deleted=0 AND has_error=0";

            using (var rdr = GetDataReader(sql, 1200))
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    while (rdr.Read())
                    {
                        yield return new WebPage()
                        {
                            WebsiteId = GetInt(rdr, "website_id"),
                            WebsitesPageId = GetInt(rdr, "websites_pages_aggregate_id"),
                            Url = GetString(rdr, "url")
                        };
                    }
                }
            }
        }

        public bool InsertWebPage(WebPage page)
        {
            var sql = "INSERT INTO websites_pages_masters (website_id, url, html, is_done, is_deleted, has_error) " +
                "VALUES ( " +
                 "" + EscapeIntegerValue(page.WebsiteId) + ", " +
                 "" + EscapeAndQuoteStringValue(page.Url) + ", " +
                 "" + EscapeAndQuoteStringValue(page.Html) + ", " +
                 "" + EscapeBooleanValue(page.IsDone) + ", " +
                 "" + EscapeBooleanValue(page.IsDeleted) + ", " +
                 "" + EscapeBooleanValue(page.HasError) + " )";
            return ExecuteNonQuery(sql);
        }
    }
}
