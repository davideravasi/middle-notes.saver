using Middle_Notes.Saver.Models;
using System.Collections.Generic;

namespace Middle_Notes.Saver.Repositories
{
    public class WebPagesAggregatesRepository : BaseRepository
    {
        public WebPagesAggregatesRepository(string connectionString) : base(connectionString)
        {
        }

        public IEnumerable<WebPage> GetWebPagesToSave(long websiteId)
        {
            var sql = "SELECT website_id, websites_pages_aggregate_id, url FROM websites_pages_aggregates " +
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

        public IEnumerable<WebPage> GetWebPagesToUpdate(long websiteId)
        {
            var sql = "SELECT website_id, websites_pages_aggregate_id, url FROM websites_pages_aggregates " +
                "WHERE website_id=" + EscapeIntegerValue(websiteId) + " " +
                "AND is_done=1 AND is_deleted=0 AND has_error=0";

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

        public bool UpdateWebPage(WebPage page)
        {
            var sql = "UPDATE websites_pages_aggregates SET html=" +
                    EscapeAndQuoteStringValue(page.Html) + ", is_done=" +
                    EscapeBooleanValue(page.IsDone) +
                    ", is_deleted = " +
                    EscapeBooleanValue(page.IsDeleted) +
                    ", has_error = " +
                    EscapeBooleanValue(page.HasError) +
                    " WHERE websites_pages_aggregate_id =" +
                    EscapeIntegerValue(page.WebsitesPageId);
            return ExecuteNonQuery(sql);
        }

        public bool ExistsWebPage(int websiteId, string url)
        {
            var sql = "SELECT COUNT(*) AS count FROM websites_pages_aggregates WHERE website_id=" + EscapeIntegerValue(websiteId)
                    + " AND url=" + EscapeAndQuoteStringValue(url);
            using (var rdr = GetDataReader(sql))
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    while (rdr.Read())
                    {
                        return GetBoolean(rdr, "count");
                    }
                }
            }
            return false;
        }

        public bool InsertWebPage(WebPage page)
        {
            var sql = "INSERT INTO websites_pages_aggregates (website_id, url, html, is_done, is_deleted, has_error) " +
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
